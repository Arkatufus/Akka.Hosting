﻿using System;
using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Hosting;

#nullable enable
namespace Akka.Persistence.PostgreSql.Hosting
{
    /// <summary>
    /// Extension methods for Akka.Persistence.PostgreSql
    /// </summary>
    public static class AkkaPersistencePostgreSqlHostingExtensions
    {
        /// <summary>
        ///     Add Akka.Persistence.PostgreSql support to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="connectionString">
        ///     Connection string used for database access.
        /// </param>
        /// <param name="mode">
        ///     Determines which settings should be added by this method call.
        /// </param>
        /// <param name="schemaName">
        ///     The schema name for the journal and snapshot store table.
        /// </param>
        /// <param name="autoInitialize">
        ///     Should the SQL store table be initialized automatically.
        /// </param>
        /// <param name="storedAsType">
        ///     Determines how data are being de/serialized into the table.
        /// </param>
        /// <param name="sequentialAccess">
        ///     Uses the `CommandBehavior.SequentialAccess` when creating SQL commands, providing a performance
        ///     improvement for reading large BLOBS.
        /// </param>
        /// <param name="useBigintIdentityForOrderingColumn">
        ///     When set to true, persistence will use `BIGINT` and `GENERATED ALWAYS AS IDENTITY` for journal table
        ///     schema creation.
        /// </param>
        /// <param name="journalBuilder">
        ///     <para>
        ///         An <see cref="Action{T}"/> used to configure an <see cref="AkkaPersistenceJournalBuilder"/> instance.
        ///     </para>
        ///     <b>Default</b>: <c>null</c>
        /// </param>
        /// <param name="isDefaultPlugin">
        ///     <para>
        ///         A <c>bool</c> flag to set the plugin as the default persistence plugin for the <see cref="ActorSystem"/>
        ///     </para>
        ///     <b>Default</b>: <c>true</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            string connectionString,
            PersistenceMode mode = PersistenceMode.Both,
            string schemaName = "public",
            bool autoInitialize = false,
            StoredAsType storedAsType = StoredAsType.ByteA,
            bool sequentialAccess = false,
            bool useBigintIdentityForOrderingColumn = false, 
            Action<AkkaPersistenceJournalBuilder>? journalBuilder = null,
            bool isDefaultPlugin = true)
        {
            if (mode == PersistenceMode.SnapshotStore && journalBuilder is { })
                throw new Exception($"{nameof(journalBuilder)} can only be set when {nameof(mode)} is set to either {PersistenceMode.Both} or {PersistenceMode.Journal}");
            
            var journalOpt = new PostgreSqlJournalOptions(isDefaultPlugin)
            {
                ConnectionString = connectionString,
                SchemaName = schemaName,
                AutoInitialize = autoInitialize,
                StoredAs = storedAsType,
                SequentialAccess = sequentialAccess,
                UseBigIntIdentityForOrderingColumn = useBigintIdentityForOrderingColumn
            };

            var adapters = new AkkaPersistenceJournalBuilder(journalOpt.Identifier, builder);
            journalBuilder?.Invoke(adapters);
            journalOpt.Adapters = adapters;

            var snapshotOpt = new PostgreSqlSnapshotOptions(isDefaultPlugin)
            {
                ConnectionString = connectionString,
                SchemaName = schemaName,
                AutoInitialize = autoInitialize,
                StoredAs = storedAsType,
                SequentialAccess = sequentialAccess
            };

            return mode switch
            {
                PersistenceMode.Journal => builder.WithPostgreSqlPersistence(journalOpt, null),
                PersistenceMode.SnapshotStore => builder.WithPostgreSqlPersistence(null, snapshotOpt),
                PersistenceMode.Both => builder.WithPostgreSqlPersistence(journalOpt, snapshotOpt),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid PersistenceMode defined.")
            };
        }

        /// <summary>
        ///     Add Akka.Persistence.PostgreSql support to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="snapshotOptionConfigurator">
        ///     An <see cref="Action{T}"/> that modifies an instance of <see cref="PostgreSqlSnapshotOptions"/>,
        ///     used to configure the snapshot store plugin
        /// </param>
        /// <param name="journalOptionConfigurator">
        ///     An <see cref="Action{T}"/> that modifies an instance of <see cref="PostgreSqlJournalOptions"/>,
        ///     used to configure the journal plugin
        /// </param>
        /// <param name="isDefaultPlugin">
        ///     <para>
        ///         A <c>bool</c> flag to set the plugin as the default persistence plugin for the <see cref="ActorSystem"/>
        ///     </para>
        ///     <b>Default</b>: <c>true</c>
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            Action<PostgreSqlJournalOptions>? journalOptionConfigurator = null,
            Action<PostgreSqlSnapshotOptions>? snapshotOptionConfigurator = null,
            bool isDefaultPlugin = true)
        {
            if (journalOptionConfigurator is null && snapshotOptionConfigurator is null)
                throw new ArgumentException($"{nameof(journalOptionConfigurator)} and {nameof(snapshotOptionConfigurator)} could not both be null");
            
            PostgreSqlJournalOptions? journalOptions = null;
            if(journalOptionConfigurator is { })
            {
                journalOptions = new PostgreSqlJournalOptions(isDefaultPlugin);
                journalOptionConfigurator(journalOptions);
            }

            PostgreSqlSnapshotOptions? snapshotOptions = null;
            if (snapshotOptionConfigurator is { })
            {
                snapshotOptions = new PostgreSqlSnapshotOptions(isDefaultPlugin);
                snapshotOptionConfigurator(snapshotOptions);
            }

            return builder.WithPostgreSqlPersistence(journalOptions, snapshotOptions);
        }

        /// <summary>
        ///     Add Akka.Persistence.PostgreSql support to the <see cref="ActorSystem"/>
        /// </summary>
        /// <param name="builder">
        ///     The builder instance being configured.
        /// </param>
        /// <param name="snapshotOptions">
        ///     An instance of <see cref="PostgreSqlSnapshotOptions"/>, used to configure the snapshot store plugin
        /// </param>
        /// <param name="journalOptions">
        ///     An instance of <see cref="PostgreSqlJournalOptions"/>, used to configure the journal plugin
        /// </param>
        /// <returns>
        ///     The same <see cref="AkkaConfigurationBuilder"/> instance originally passed in.
        /// </returns>
        public static AkkaConfigurationBuilder WithPostgreSqlPersistence(
            this AkkaConfigurationBuilder builder,
            PostgreSqlJournalOptions? journalOptions = null,
            PostgreSqlSnapshotOptions? snapshotOptions = null)
        {
            if (journalOptions is null && snapshotOptions is null)
                throw new ArgumentException($"{nameof(journalOptions)} and {nameof(snapshotOptions)} could not both be null");
            
            return (journalOptions, snapshotOptions) switch
            {
                (null, null) => 
                    throw new ArgumentException($"{nameof(journalOptions)} and {nameof(snapshotOptions)} could not both be null"),
                
                (_, null) => 
                    builder
                        .AddHocon(journalOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(PostgreSqlPersistence.DefaultConfiguration(), HoconAddMode.Append),
                
                (null, _) => 
                    builder
                        .AddHocon(snapshotOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(PostgreSqlPersistence.DefaultConfiguration(), HoconAddMode.Append),
                
                (_, _) => 
                    builder
                        .AddHocon(journalOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(snapshotOptions.ToConfig(), HoconAddMode.Prepend)
                        .AddHocon(PostgreSqlPersistence.DefaultConfiguration(), HoconAddMode.Append),
            };
        }
        
    }
}

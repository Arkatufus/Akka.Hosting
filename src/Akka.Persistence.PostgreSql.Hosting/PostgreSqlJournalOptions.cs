﻿// -----------------------------------------------------------------------
//  <copyright file="PostgreSqlJournalOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;
using Akka.Persistence.Hosting;

#nullable enable
namespace Akka.Persistence.PostgreSql.Hosting
{
    /// <summary>
    /// Akka.Hosting options class to set up PostgreSql persistence journal.
    /// </summary>
    public sealed class PostgreSqlJournalOptions: SqlJournalOptions
    {
        private static readonly Config Default = PostgreSqlPersistence.DefaultConfiguration()
            .GetConfig(PostgreSqlJournalSettings.JournalConfigPath);
        
        /// <summary>
        /// Create a new instance of <see cref="PostgreSqlJournalOptions"/>
        /// </summary>
        /// <param name="isDefaultPlugin">Indicates if this journal configuration should be the default configuration for all persistence</param>
        /// <param name="identifier">The journal configuration identifier, <b>default</b>: "postgresql"</param>
        public PostgreSqlJournalOptions(bool isDefaultPlugin, string identifier = "postgresql") : base(isDefaultPlugin)
        {
            Identifier = identifier;
        }
        
        /// <summary>
        ///     <para>
        ///         The plugin identifier for this persistence plugin
        ///     </para>
        ///     <b>Default</b>: "postgresql"
        /// </summary>
        public override string Identifier { get; set; }
        
        /// <summary>
        ///     <para>
        ///         PostgreSQL schema name to table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: "public"
        /// </summary>
        public override string SchemaName { get; set; } = "public";
        
        /// <summary>
        ///     <para>
        ///         PostgreSQL table corresponding with persistent journal.
        ///     </para>
        ///     <b>Default</b>: "event_journal"
        /// </summary>
        public override string TableName { get; set; } = "event_journal";
        
        /// <summary>
        ///     <para>
        ///         PostgreSQL table corresponding with persistent journal metadata.
        ///     </para>
        ///     <b>Default</b>: "metadata"
        /// </summary>
        public override string MetadataTableName { get; set; } = "metadata";

        /// <summary>
        ///     <para>
        ///         Uses the CommandBehavior.SequentialAccess when creating DB commands, providing a performance
        ///         improvement for reading large BLOBS.
        ///     </para>
        ///     <b>Default</b>: false
        /// </summary>
        public override bool SequentialAccess { get; set; } = false;
        
        /// <summary>
        /// Postgres data type for payload column
        /// </summary>
        public StoredAsType StoredAs { get; set; } = StoredAsType.ByteA;
        
        /// <summary>
        /// When turned on, persistence will use `BIGINT` and `GENERATED ALWAYS AS IDENTITY` for the ordering column
        /// in the journal table during schema creation.
        /// </summary>
        public bool UseBigIntIdentityForOrderingColumn { get; set; } = false;

        protected override Config InternalDefaultConfig => Default;

        protected override StringBuilder Build(StringBuilder sb)
        {
            sb.AppendLine($"use-bigint-identity-for-ordering-column = {(UseBigIntIdentityForOrderingColumn ? "on" : "off")}");
            sb.AppendLine($"stored-as = {StoredAs.ToHocon()}");
            
            return base.Build(sb);
        }
    }
}
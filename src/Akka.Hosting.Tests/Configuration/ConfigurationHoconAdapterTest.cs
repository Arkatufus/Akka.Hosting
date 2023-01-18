// -----------------------------------------------------------------------
//  <copyright file="ConfigurationHoconAdapterTest.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Akka.Hosting.Configuration;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Akka.Hosting.Tests.Configuration;

public class ConfigurationHoconAdapterTest
{
    private const string ConfigSource = @"{
  ""akka"": {
    ""cluster"": {
      ""roles"": [ ""front-end"", ""back-end"" ],
      ""role"" : {
        ""back-end"" : 5
      },
      ""app-version"": ""1.0.0"",
      ""min-nr-of-members"": 99,
      ""seed-nodes"": [ ""akka.tcp://system@somewhere.com:9999"" ],
      ""log-info"": false,
      ""log-info-verbose"": true
    }
  },
  ""test1"": ""test1 content"",
  ""test2.a"": ""on"",
  ""test2.b.c"": ""2s"",
  ""test2.b.d"": ""test2.b.d content"",
  ""test2.d"": ""test2.d content"",
  ""test3"": ""3"",
  ""test4"": 4
}";

    private readonly IConfigurationBuilder _builder;

    public ConfigurationHoconAdapterTest()
    {
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__A", "A VALUE");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__B", "B VALUE");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__C__D", "D");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__0", "ZERO");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__22", "TWO");
        Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__1", "ONE");
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ConfigSource));
        _builder = new ConfigurationBuilder()
            .AddJsonStream(stream);
    }

    [Fact(DisplayName = "Adaptor should read environment variable sourced configuration correctly")]
    public void EnvironmentVariableTest()
    {
        Environment.SetEnvironmentVariable("Akka__Test_Value_1__A", "A VALUE");
        Environment.SetEnvironmentVariable("Akka__Test_Value_1__B", "B VALUE");
        Environment.SetEnvironmentVariable("Akka__Test_Value_1__C__D", "D");
        Environment.SetEnvironmentVariable("Akka__Test_Value_2__0", "ZERO");
        Environment.SetEnvironmentVariable("Akka__Test_Value_2__22", "TWO");
        Environment.SetEnvironmentVariable("Akka__Test_Value_2__1", "ONE");

        try
        {
            var configRoot = _builder.AddEnvironmentVariables().Build();
            var config = configRoot.ToHocon();
            
            config.GetString("akka.test-value-1.a").Should().Be("A VALUE");
            config.GetString("akka.test-value-1.b").Should().Be("B VALUE");
            config.GetString("akka.test-value-1.c.d").Should().Be("D");
            var array = config.GetStringList("akka.test-value-2");
            array[0].Should().Be("ZERO");
            array[1].Should().Be("ONE");
            array[2].Should().Be("TWO");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__A", null);
            Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__B", null);
            Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_1__C__D", null);
            Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__0", null);
            Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__22", null);
            Environment.SetEnvironmentVariable("AKKA__TEST_VALUE_2__1", null);
        }
    }
    
    [Fact(DisplayName = "Adapter should bind environment variable sourced configuration correctly")]
    public void EnvironmentVariableBindTest()
    {
        Environment.SetEnvironmentVariable("Akka__TestValues__0", "0");
        Environment.SetEnvironmentVariable("Akka__TestValues__1", "1");
        Environment.SetEnvironmentVariable("Akka__TestValues__2", "2");
        Environment.SetEnvironmentVariable("Akka__TestString", "ZERO");

        try
        {
            var config = _builder.AddEnvironmentVariables().Build();
            var bound = config.GetSection("Akka").Get<BindTest>();
            bound.TestValues.Should().BeEquivalentTo( new []{0, 1, 2});
            bound.TestString.Should().Be("ZERO");
            bound.EmptyOne.Should().BeNull();
            bound.EmptyTwo.Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("Akka__TestValues__0", null);
            Environment.SetEnvironmentVariable("Akka__TestValues__1", null);
            Environment.SetEnvironmentVariable("Akka__TestValues__2", null);
            Environment.SetEnvironmentVariable("Akka__TestString", null);
        }
    }
    
    [Fact(DisplayName = "Adaptor should expand keys")]
    public void EncodedKeyTest()
    {
        var config = _builder.Build().ToHocon();
        
        var test2 = config.GetConfig("test2");
        test2.Should().NotBeNull();
        test2.GetBoolean("a").Should().BeTrue();
        test2.GetTimeSpan("b.c").Should().Be(2.Seconds());
        test2.GetString("b.d").Should().Be("test2.b.d content");
        test2.GetString("d").Should().Be("test2.d content");
    }

    [Fact(DisplayName = "Adaptor should convert correctly")]
    public void ArrayTest()
    {
        var config = _builder.Build().ToHocon();
        
        config.GetString("test1").Should().Be("test1 content");
        config.GetInt("test3").Should().Be(3);
        config.GetInt("test4").Should().Be(4);
        
        config.GetStringList("akka.cluster.roles").Should().BeEquivalentTo("front-end", "back-end");
        config.GetInt("akka.cluster.role.back-end").Should().Be(5);
        config.GetString("akka.cluster.app-version").Should().Be("1.0.0");
        config.GetInt("akka.cluster.min-nr-of-members").Should().Be(99);
        config.GetStringList("akka.cluster.seed-nodes").Should()
            .BeEquivalentTo("akka.tcp://system@somewhere.com:9999");
        config.GetBoolean("akka.cluster.log-info").Should().BeFalse();
        config.GetBoolean("akka.cluster.log-info-verbose").Should().BeTrue();
    }
    
    private class BindTest
    {
        public int[]? TestValues { get; set; }
        public string? TestString { get; set; }
        public int? EmptyOne { get; set; }
        public string? EmptyTwo { get; set; }
    }
}
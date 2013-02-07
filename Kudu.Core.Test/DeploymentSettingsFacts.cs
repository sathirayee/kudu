﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Kudu.Contracts.Settings;
using Kudu.Core.Settings;
using Moq;
using XmlSettings;
using Xunit;
using Xunit.Extensions;

namespace Kudu.Core.Test
{
    public class DeploymentSettingFacts
    {
        [Theory, ClassData(typeof(CommandIdleTimeoutData))]
        public void CommandIdleTimeoutTests(string value, int expected)
        {
            // Act
            MockDeploymentSettingsManager settings = new MockDeploymentSettingsManager();
            settings.SetValue(SettingsKeys.CommandIdleTimeout, value);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(expected), settings.GetCommandIdleTimeout());
        }

        [Theory, ClassData(typeof(LogStreamTimeoutData))]
        public void LogStreamTimeoutTests(string value, int expected)
        {
            // Act
            MockDeploymentSettingsManager settings = new MockDeploymentSettingsManager();
            settings.SetValue(SettingsKeys.LogStreamTimeout, value);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(expected), settings.GetLogStreamTimeout());
        }

        [Theory, ClassData(typeof(TraceLevelData))]
        public void TraceLevelTests(string value, TraceLevel expected)
        {
            // Act
            MockDeploymentSettingsManager settings = new MockDeploymentSettingsManager();
            settings.SetValue(SettingsKeys.TraceLevel, value);

            // Assert
            Assert.Equal(expected, settings.GetTraceLevel());
        }

        [Fact]
        public void GetBranchUsesPersistedBranchValueIfAvailable()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("deployment", "branch")).Returns("my-branch");
            var deploymentSettings = new DeploymentSettingsManager(settings.Object, new Dictionary<string, string>());

            // Act
            string branch = deploymentSettings.GetBranch();

            // Assert
            Assert.Equal("my-branch", branch);
        }

        [Fact]
        public void GetBranchUsesDeploymentBranchIfLegacyValueIsUnavailable()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("deployment", "deployment_branch")).Returns("my-branch");
            var deploymentSettings = new DeploymentSettingsManager(settings.Object, new Dictionary<string, string>());

            // Act
            string branch = deploymentSettings.GetBranch();

            // Assert
            Assert.Equal("my-branch", branch);
        }

        [Fact]
        public void GetBranchUsesLegacyBranchValueIfDeploymentBranchIsOnlyAvailableAsPartOfEnvironment()
        {
            // Arrange
            var settings = new Mock<ISettings>();
            var defaultSettings = new Dictionary<string, string> 
            {
                { "deployment_branch", "my-deployment-branch" }
            };
            settings.Setup(s => s.GetValue("deployment", "branch")).Returns("my-branch");
            var deploymentSettings = new DeploymentSettingsManager(settings.Object, defaultSettings);

            // Act
            string branch = deploymentSettings.GetBranch();

            // Assert
            Assert.Equal("my-branch", branch);
        }

        [Fact]
        public void GetBranchDoesNotUnifyValuesWithLegacyKey()
        {
            // Arrange
            var settings = Mock.Of<ISettings>();
            var defaultSettings = new Dictionary<string, string> 
            {
                { "deployment_branch", "my-deployment-branch" },
                { "branch", "my-legacy-branch" }
            };
            var deploymentSettings = new DeploymentSettingsManager(settings, defaultSettings);

            // Act
            string branch = deploymentSettings.GetBranch();

            // Assert
            Assert.Equal("my-deployment-branch", branch);
        }

        [Fact]
        public void SetBranchClearsLegacyKeyIfPresent()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.DeleteValue("deployment", "branch")).Returns(true).Verifiable();
            settings.Setup(s => s.SetValue("deployment", "deployment_branch", "my-branch")).Verifiable();
            var deploymentSettings = new DeploymentSettingsManager(settings.Object, new Dictionary<string, string>());

            // Act
            deploymentSettings.SetBranch("my-branch");

            // Assert
            settings.Verify();
        }

        class CommandIdleTimeoutData : SettingsData
        {
            protected override object DefaultValue
            {
                get { return (int)DeploymentSettingsExtension.DefaultCommandIdleTimeout.TotalSeconds; }
            }
        }

        class LogStreamTimeoutData : SettingsData
        {
            protected override object DefaultValue
            {
                get { return (int)DeploymentSettingsExtension.DefaultLogStreamTimeout.TotalSeconds; }
            }
        }

        class TraceLevelData : SettingsData
        {
            protected override object DefaultValue
            {
                get { return DeploymentSettingsExtension.DefaultTraceLevel; }
            }
        }

        abstract class SettingsData : IEnumerable<object[]>
        {
            protected abstract object DefaultValue { get; }

            public IEnumerator<object[]> GetEnumerator()
            { 
                yield return new object[] { "0", 0 };
                yield return new object[] { "-0", 0 };
                yield return new object[] { "4", 4 };
                yield return new object[] { "-4", 0 };
                yield return new object[] { "", DefaultValue };
                yield return new object[] { "a", DefaultValue };
                yield return new object[] { " ", DefaultValue };
                yield return new object[] { null, DefaultValue };
            }

            IEnumerator IEnumerable.GetEnumerator()
            { 
                return GetEnumerator(); 
            }
        }
    }
}
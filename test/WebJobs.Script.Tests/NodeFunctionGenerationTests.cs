﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Script;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebJobs.Script.Tests
{
    public class NodeFunctionGenerationTests
    {
        [Fact]
        public void GenerateTimerTriggerFunction()
        {
            FunctionInfo function = new FunctionInfo();
            function.Name = "Test";
            function.Source = Path.Combine(Environment.CurrentDirectory, @"scripts\Common\test.js");
            JObject trigger = new JObject
            {
                { "type", "timer" },
                { "schedule", "* * * * * *" },
                { "runOnStartup", true }
            };
            function.Configuration = new JObject
            {
                { "trigger", trigger }
            };
            MethodInfo method = GenerateMethod(function);

            VerifyCommonProperties(method);

            // verify trigger parameter
            ParameterInfo parameter = method.GetParameters()[0];
            Assert.Equal("input", parameter.Name);
            Assert.Equal(typeof(TimerInfo), parameter.ParameterType);
            TimerTriggerAttribute attribute = parameter.GetCustomAttribute<TimerTriggerAttribute>();
            Assert.Equal("Cron: '* * * * * *'", attribute.Schedule.ToString());
            Assert.True(attribute.RunOnStartup);
        }

        [Fact]
        public void GenerateQueueTriggerFunction()
        {
            FunctionInfo function = new FunctionInfo();
            function.Name = "Test";
            function.Source = Path.Combine(Environment.CurrentDirectory, @"scripts\Common\test.js");
            JObject trigger = new JObject
            {
                { "type", "queue" },
                { "queueName", "test" }
            };
            function.Configuration = new JObject
            {
                { "trigger", trigger }
            };
            MethodInfo method = GenerateMethod(function);

            VerifyCommonProperties(method);

            // verify trigger parameter
            ParameterInfo parameter = method.GetParameters()[0];
            Assert.Equal("input", parameter.Name);
            Assert.Equal(typeof(string), parameter.ParameterType);
            QueueTriggerAttribute attribute = parameter.GetCustomAttribute<QueueTriggerAttribute>();
            Assert.Equal("test", attribute.QueueName);
        }

        [Fact]
        public void GenerateBlobTriggerFunction()
        {
            FunctionInfo function = new FunctionInfo();
            function.Name = "Test";
            function.Source = Path.Combine(Environment.CurrentDirectory, @"scripts\Common\test.js");
            JObject trigger = new JObject
            {
                { "type", "blob" },
                { "blobPath", "foo/bar" }
            };
            function.Configuration = new JObject
            {
                { "trigger", trigger }
            };
            MethodInfo method = GenerateMethod(function);

            VerifyCommonProperties(method);

            // verify trigger parameter
            ParameterInfo parameter = method.GetParameters()[0];
            Assert.Equal("input", parameter.Name);
            Assert.Equal(typeof(string), parameter.ParameterType);
            BlobTriggerAttribute attribute = parameter.GetCustomAttribute<BlobTriggerAttribute>();
            Assert.Equal("foo/bar", attribute.BlobPath);
        }

        [Fact]
        public void GenerateWebHookTriggerFunction()
        {
            FunctionInfo function = new FunctionInfo();
            function.Name = "Test";
            function.Source = Path.Combine(Environment.CurrentDirectory, @"scripts\Common\test.js");
            JObject trigger = new JObject
            {
                { "type", "webHook" }
            };
            function.Configuration = new JObject
            {
                { "trigger", trigger }
            };
            MethodInfo method = GenerateMethod(function);

            VerifyCommonProperties(method);

            // verify trigger parameter
            ParameterInfo parameter = method.GetParameters()[0];
            Assert.Equal("input", parameter.Name);
            Assert.Equal(typeof(string), parameter.ParameterType);
            WebHookTriggerAttribute attribute = parameter.GetCustomAttribute<WebHookTriggerAttribute>();
            Assert.NotNull(attribute);
        }

        [Fact]
        public void GenerateServiceBusTriggerFunction()
        {
            FunctionInfo function = new FunctionInfo();
            function.Name = "Test";
            function.Source = Path.Combine(Environment.CurrentDirectory, @"scripts\Common\test.js");
            JObject trigger = new JObject
            {
                { "type", "serviceBus" },
                { "topicName", "testTopic" },
                { "subscriptionName", "testSubscription" },
                { "accessRights", "listen" }
            };
            function.Configuration = new JObject
            {
                { "trigger", trigger }
            };
            MethodInfo method = GenerateMethod(function);

            VerifyCommonProperties(method);

            // verify trigger parameter
            ParameterInfo parameter = method.GetParameters()[0];
            Assert.Equal("input", parameter.Name);
            Assert.Equal(typeof(string), parameter.ParameterType);
            ServiceBusTriggerAttribute attribute = parameter.GetCustomAttribute<ServiceBusTriggerAttribute>();
            Assert.Equal(null, attribute.QueueName);
            Assert.Equal("testTopic", attribute.TopicName);
            Assert.Equal("testSubscription", attribute.SubscriptionName);
            Assert.Equal(AccessRights.Listen, attribute.Access);
        }

        private static void VerifyCommonProperties(MethodInfo method)
        {
            Assert.Equal("Test", method.Name);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal(typeof(Task), method.ReturnType);

            // verify TextWriter parameter
            ParameterInfo parameter = parameters[1];
            Assert.Equal("log", parameter.Name);
            Assert.Equal(typeof(TextWriter), parameter.ParameterType);

            // verify IBinder parameter
            parameter = parameters[2];
            Assert.Equal("binder", parameter.Name);
            Assert.Equal(typeof(IBinder), parameter.ParameterType);
        }

        private static MethodInfo GenerateMethod(FunctionInfo function)
        {
            List<FunctionInfo> functions = new List<FunctionInfo>();
            functions.Add(function);

            FunctionDescriptorProvider[] descriptorProviders = new FunctionDescriptorProvider[]
            {
                new NodeFunctionDescriptorProvider(Environment.CurrentDirectory)
            };
            var functionDescriptors = ScriptHost.ReadFunctions(functions, descriptorProviders);
            Type t = FunctionGenerator.Generate("Host.Functions", functionDescriptors);

            MethodInfo method = t.GetMethods(BindingFlags.Public | BindingFlags.Static).First();
            return method;
        }
    }
}
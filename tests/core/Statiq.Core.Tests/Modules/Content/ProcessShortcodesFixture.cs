﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Statiq.Common;
using Statiq.Testing;

namespace Statiq.Core.Tests.Modules.Contents
{
    [TestFixture]
    public class ProcessShortcodesFixture : BaseFixture
    {
        public class ExecuteTests : ProcessShortcodesFixture
        {
            [Test]
            public async Task ProcessesShortcode()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<TestShortcode>("Bar");
                TestDocument document = new TestDocument("123<?# Bar /?>456");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123Foo456");
            }

            [Test]
            public async Task ProcessesNestedShortcodeInResult()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<TestShortcode>("Nested");
                context.Shortcodes.Add<NestedShortcode>("Bar");
                TestDocument document = new TestDocument("123<?# Bar /?>456");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123ABCFooXYZ456");
            }

            [Test]
            public async Task ProcessesNestedShortcode()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<RawShortcode>("Foo");
                context.Shortcodes.Add<TestShortcode>("Bar");
                TestDocument document = new TestDocument("123<?# Foo ?>ABC<?# Bar /?>XYZ<?#/ Foo ?>456");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123ABCFooXYZ456");
            }

            [Test]
            public async Task DoesNotProcessNestedRawShortcode()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<RawShortcode>("Raw");
                context.Shortcodes.Add<TestShortcode>("Bar");
                TestDocument document = new TestDocument("123<?# Raw ?>ABC<?# Bar /?>XYZ<?#/ Raw ?>456");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123ABC<?# Bar /?>XYZ456");
            }

            [Test]
            public async Task DoesNotProcessDirectlyNestedRawShortcode()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<RawShortcode>("Raw");
                context.Shortcodes.Add<TestShortcode>("Bar");
                TestDocument document = new TestDocument("123<?# Raw ?><?# Bar /?><?#/ Raw ?>456");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123<?# Bar /?>456");
            }

            [Test]
            public async Task ShortcodeSupportsNullResult()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<TestShortcode>("S1");
                context.Shortcodes.Add<NullResultShortcode>("S2");
                TestDocument document = new TestDocument("123<?# S1 /?>456<?# S2 /?>789<?# S1 /?>");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123Foo456789Foo");
            }

            [Test]
            public async Task DisposesShortcode()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<DisposableShortcode>("Bar");
                TestDocument document = new TestDocument("123<?# Bar /?>456");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                DisposableShortcode.Disposed.ShouldBeTrue();
            }

            [Test]
            public async Task ShortcodesCanAddMetadata()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<AddsMetadataShortcode>("S1");
                context.Shortcodes.Add<AddsMetadataShortcode2>("S2");
                TestDocument document = new TestDocument("123<?# S1 /?>456<?# S2 /?>789");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123456789");
                result["A"].ShouldBe("3");
                result["B"].ShouldBe("2");
                result["C"].ShouldBe("4");
            }

            [Test]
            public async Task ShortcodesCanReadMetadata()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<ReadsMetadataShortcode>("S1");
                context.Shortcodes.Add<ReadsMetadataShortcode>("S2");
                TestDocument document = new TestDocument("123<?# S1 /?>456<?# S2 /?>789<?# S1 /?>")
                {
                    { "Foo", 10 }
                };
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123456789");
                result["Foo"].ShouldBe(13);
            }

            [Test]
            public async Task ShortcodesPersistState()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<IncrementingShortcode>("S");
                TestDocument document = new TestDocument("123<?# S /?>456<?# S /?>789<?# S /?>");
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123456789");
                result["Foo"].ShouldBe(22);
            }

            [Test]
            public async Task MultipleShortcodeResultDocuments()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Shortcodes.Add<MultiShortcode>("S");
                TestDocument document = new TestDocument("123<?# S /?>456")
                {
                    { "Foo", 10 }
                };
                ProcessShortcodes module = new ProcessShortcodes();

                // When
                TestDocument result = await ExecuteAsync(document, context, module).SingleAsync();

                // Then
                result.Content.ShouldBe("123aaaBBB456");
                result["Foo"].ShouldBe(12);
            }
        }

        public class TestShortcode : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new TestDocument("Foo").Yield());
        }

        public class NestedShortcode : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new TestDocument("ABC<?# Nested /?>XYZ").Yield());
        }

        public class RawShortcode : IShortcode
        {
            public async Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                new TestDocument(await context.GetContentStreamAsync(content)).Yield();
        }

        public class NullResultShortcode : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) => Task.FromResult<IEnumerable<IDocument>>(null);
        }

        public class DisposableShortcode : IShortcode, IDisposable
        {
            public static bool Disposed { get; set; }

            public DisposableShortcode()
            {
                // Make sure it resets
                Disposed = false;
            }

            public async Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                new TestDocument(await context.GetContentStreamAsync("Foo")).Yield();

            public void Dispose() =>
                Disposed = true;
        }

        public class AddsMetadataShortcode : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new TestDocument(new MetadataItems
                {
                    { "A", "1" },
                    { "B", "2" }
                }).Yield());
        }

        public class AddsMetadataShortcode2 : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new TestDocument(new MetadataItems
                {
                    { "A", "3" },
                    { "C", "4" }
                }).Yield());
        }

        public class ReadsMetadataShortcode : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new TestDocument(new MetadataItems
                {
                    { $"Foo", document.GetInt("Foo") + 1 }
                }).Yield());
        }

        public class IncrementingShortcode : IShortcode
        {
            private int _value = 20;

            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new TestDocument(new MetadataItems
                {
                    { $"Foo", _value++ }
                }).Yield());
        }

        public class MultiShortcode : IShortcode
        {
            public Task<IEnumerable<IDocument>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
                Task.FromResult<IEnumerable<IDocument>>(new IDocument[]
                {
                    new TestDocument("aaa")
                    {
                        { $"Foo", document.GetInt("Foo") + 1 }
                    },
                    new TestDocument("BBB")
                    {
                        { $"Foo", document.GetInt("Foo") + 2 }
                    }
                });
        }
    }
}

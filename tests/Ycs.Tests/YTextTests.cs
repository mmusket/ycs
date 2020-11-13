﻿// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ycs
{
    [TestClass]
    public class YTextTests : YTestBase
    {
        [TestMethod]
        public void Sample()
        {
            var doc = new YDoc();
            var text = doc.GetText("monaco");
            text.Insert(0, "hello");
            var buff = doc.EncodeStateAsUpdateV2(new byte[] { 0 });

            //doc.ApplyUpdateV2(new byte[] { 0, 0, 5, 144, 207, 169, 140, 4, 0, 0, 1, 4, 14, 11, 109, 111, 110, 97, 99, 111, 104, 101, 108, 108, 111, 6, 5, 1, 1, 0, 0, 1, 1, 0, 0 });
            //var text = doc.GetText("monaco");
            //var str = text.ToString();
        }

        [TestMethod]
        public void TestBasicInsertAndDelete()
        {
            Init(users: 2);
            var text0 = Texts[Users[0]];

            IList<Delta> delta = null;
            text0.EventHandler += (s, e) =>
            {
                delta = (e.Event as YTextEvent).Delta;
            };

            // Does not throw when deleting zero elements with position 0.
            text0.Delete(0, 0);

            text0.Insert(0, "abc");
            Assert.AreEqual("abc", text0.ToString());
            Assert.AreEqual("abc", delta[0].Insert[0]);
            delta = null;

            text0.Delete(0, 1);
            Assert.AreEqual("bc", text0.ToString());
            Assert.AreEqual(1, delta[0].Delete);
            delta = null;

            text0.Delete(1, 1);
            Assert.AreEqual("b", text0.ToString());
            Assert.AreEqual(1, delta[0].Retain);
            Assert.AreEqual(1, delta[1].Delete);
            delta = null;

            Users[0].Transact(tr =>
            {
                text0.Insert(0, "1");
                text0.Delete(0, 1);
            });

            CompareUsers();
        }

        [TestMethod]
        public void TestBasicFormat()
        {
            Init(users: 2);
            var text0 = Texts[Users[0]];

            IList<Delta> delta = null;
            text0.EventHandler += (s, e) =>
            {
                delta = (e.Event as YTextEvent).Delta;
            };

            text0.Insert(0, "abc", new Dictionary<string, object> { { "bold", true } });
            Assert.AreEqual("abc", text0.ToString());

            var text0Delta = text0.ToDelta();
            Assert.AreEqual(1, text0Delta?.Count);
            CollectionAssert.AreEqual(new[] { "abc" }, (ICollection)text0Delta[0].Insert);
            Assert.AreEqual(true, text0Delta[0].Attributes["bold"]);

            text0.Delete(0, 1);
            Assert.AreEqual("bc", text0.ToString());
            text0Delta = text0.ToDelta();
            Assert.AreEqual(1, text0Delta?.Count);
            CollectionAssert.AreEqual(new[] { "bc" }, (ICollection)text0Delta[0].Insert);
            Assert.AreEqual(true, text0Delta[0].Attributes["bold"]);
            Assert.AreEqual(2, delta?.Count);
            Assert.AreEqual(1, delta[0].Delete);
            Assert.AreEqual(2, delta[1].Retain);
            delta = null;

            text0.Delete(1, 1);
            Assert.AreEqual("b", text0.ToString());
            text0Delta = text0.ToDelta();
            Assert.AreEqual(1, text0Delta?.Count);
            CollectionAssert.AreEqual(new[] { "b" }, (ICollection)text0Delta[0].Insert);
            Assert.AreEqual(true, text0Delta[0].Attributes["bold"]);
            Assert.AreEqual(2, delta?.Count);
            Assert.AreEqual(1, delta[0].Retain);
            Assert.AreEqual(1, delta[1].Delete);
            delta = null;

            text0.Insert(0, "z", new Dictionary<string, object> { { "bold", true } });
            Assert.AreEqual("zb", text0.ToString());
            text0Delta = text0.ToDelta();
            Assert.AreEqual(1, text0Delta?.Count);
            CollectionAssert.AreEqual(new[] { "zb" }, (ICollection)text0Delta[0].Insert);
            Assert.AreEqual(true, text0Delta[0].Attributes["bold"]);
            Assert.AreEqual(2, delta?.Count);
            CollectionAssert.AreEqual(new[] { "z" }, (ICollection)delta[0].Insert);
            Assert.AreEqual(true, delta[0].Attributes["bold"]);
            Assert.AreEqual(1, delta[1].Retain);
            delta = null;

            // Check that there are no duplicate markers inserted.
            Assert.AreEqual("b", ((((text0._start.Right as Item)?.Right as Item)?.Right as Item)?.Content as ContentString)?.GetString());

            text0.Insert(0, "y");
            Assert.AreEqual("yzb", text0.ToString());
            text0Delta = text0.ToDelta();
            Assert.AreEqual(2, text0Delta?.Count);
            CollectionAssert.AreEqual(new[] { "y" }, (ICollection)text0Delta[0].Insert);
            Assert.IsNull(text0Delta[0].Attributes);
            CollectionAssert.AreEqual(new[] { "zb" }, (ICollection)text0Delta[1].Insert);
            Assert.AreEqual(true, text0Delta[1].Attributes["bold"]);
            Assert.AreEqual(2, delta?.Count);
            CollectionAssert.AreEqual(new[] { "y" }, (ICollection)delta[0].Insert);
            Assert.IsNull(delta[0].Attributes);
            Assert.AreEqual(2, delta[1].Retain);
            delta = null;

            text0.Format(0, 2, new Dictionary<string, object> { { "bold", null } });
            Assert.AreEqual("yzb", text0.ToString());
            text0Delta = text0.ToDelta();
            Assert.AreEqual(2, text0Delta?.Count);
            CollectionAssert.AreEqual(new[] { "yz" }, (ICollection)text0Delta[0].Insert);
            Assert.IsNull(text0Delta[0].Attributes);
            CollectionAssert.AreEqual(new[] { "b" }, (ICollection)text0Delta[1].Insert);
            Assert.AreEqual(true, text0Delta[1].Attributes["bold"]);
            Assert.AreEqual(3, delta?.Count);
            Assert.AreEqual(1, delta[0].Retain);
            Assert.AreEqual(1, delta[1].Retain);
            Assert.AreEqual(null, delta[1].Attributes["bold"]);
            Assert.AreEqual(1, delta[2].Retain);

            CompareUsers();
        }

        [TestMethod]
        public void TestGetDeltaWithEmbeds()
        {
            Init(users: 1);
            var text0 = Texts[Users[0]];

            text0.ApplyDelta(new[]
            {
                new Delta
                {
                    Insert = new List<object> { new Dictionary<string, string> { { "linebreak", "s" } } }
                }
            });

            var delta = text0.ToDelta();
            Assert.AreEqual(1, delta?.Count);
            Assert.AreEqual("s", (delta[0].Insert[0] as IDictionary<string, string>)["linebreak"]);
        }

        [TestMethod]
        public void TestSnapshot()
        {
            Init(users: 1, options: new YDocOptions { Gc = false });
            var text0 = Texts[Users[0]];
            var doc0 = text0.Doc;

            text0.ApplyDelta(new[]
            {
                new Delta
                {
                    Insert = new List<object> { "abcd" }
                }
            });

            var snapshot1 = doc0.CreateSnapshot();

            text0.ApplyDelta(new[]
            {
                new Delta
                {
                    Retain = 1
                },
                new Delta
                {
                    Insert = new List<object>{ "x" }
                },
                new Delta
                {
                    Delete = 1
                }
            });

            var snapshot2 = doc0.CreateSnapshot();

            text0.ApplyDelta(new[]
            {
                new Delta
                {
                    Retain = 2
                },
                new Delta
                {
                    Delete = 3
                },
                new Delta
                {
                    Insert = new List<object> { "x" }
                },
                new Delta
                {
                    Delete = 1
                }
            });

            var state1 = text0.ToDelta(snapshot1);
            Assert.AreEqual(1, state1?.Count);
            Assert.AreEqual("abcd", state1[0].Insert[0]);

            var state2 = text0.ToDelta(snapshot2);
            Assert.AreEqual(1, state2?.Count);
            Assert.AreEqual("axcd", state2[0].Insert[0]);

            var stateDiff = text0.ToDelta(snapshot2, snapshot1);
            Assert.AreEqual(4, stateDiff.Count);
            Assert.AreEqual("a", stateDiff[0].Insert[0]);

            Assert.AreEqual("x", stateDiff[1].Insert[0]);
            Assert.AreEqual(YText.YTextChangeType.Added, (stateDiff[1].Attributes["__ychange"] as YText.YTextChangeAttributes).Type);

            Assert.AreEqual("b", stateDiff[2].Insert[0]);
            Assert.AreEqual(YText.YTextChangeType.Removed, (stateDiff[2].Attributes["__ychange"] as YText.YTextChangeAttributes).Type);

            Assert.AreEqual("cd", stateDiff[3].Insert[0]);
            Assert.IsNull(stateDiff[3].Attributes);
        }

        [TestMethod]
        public void TestSnapshotDeleteAfter()
        {
            Init(users: 1, options: new YDocOptions { Gc = false });
            var text0 = Texts[Users[0]];
            var doc0 = text0.Doc;

            text0.ApplyDelta(new[]
            {
                new Delta
                {
                    Insert = new List<object> { "abcd" }
                }
            });

            var snapshot1 = doc0.CreateSnapshot();

            text0.ApplyDelta(new[]
            {
                new Delta
                {
                    Retain = 4
                },
                new Delta
                {
                    Insert = new List<object>{ "e" }
                }
            });

            var state1 = text0.ToDelta(snapshot1);
            Assert.AreEqual(1, state1?.Count);
            Assert.AreEqual("abcd", state1[0].Insert[0]);
        }

        [TestMethod]
        public void TestToDeltaEmbedAttributes()
        {
            Init(users: 1);
            var text0 = Texts[Users[0]];

            text0.Insert(0, "ab", new Dictionary<string, object> { { "bold", true } });
            text0.InsertEmbed(1, new[] { "this", "is", "embed" }, new Dictionary<string, object> { { "width", 100 } });

            var delta = text0.ToDelta();
            Assert.AreEqual(3, delta?.Count);

            Assert.AreEqual("a", delta[0].Insert[0]);
            Assert.AreEqual(true, delta[0].Attributes["bold"]);

            CollectionAssert.AreEqual(new[] { "this", "is", "embed" }, (ICollection)delta[1].Insert[0]);
            Assert.AreEqual(100, delta[1].Attributes["width"]);

            Assert.AreEqual("b", delta[2].Insert[0]);
            Assert.AreEqual(true, delta[2].Attributes["bold"]);
        }

        [TestMethod]
        public void TestToDeltaEmbedNoAttributes()
        {
            Init(users: 1);
            var text0 = Texts[Users[0]];

            text0.Insert(0, "ab", new Dictionary<string, object> { { "bold", true } });
            text0.InsertEmbed(1, new[] { "this", "is", "embed" });

            var delta = text0.ToDelta();
            Assert.AreEqual(3, delta?.Count);

            Assert.AreEqual("a", delta[0].Insert[0]);
            Assert.AreEqual(true, delta[0].Attributes["bold"]);

            CollectionAssert.AreEqual(new[] { "this", "is", "embed" }, (ICollection)delta[1].Insert[0]);
            Assert.IsNull(delta[1].Attributes);

            Assert.AreEqual("b", delta[2].Insert[0]);
            Assert.AreEqual(true, delta[2].Attributes["bold"]);
        }

        [TestMethod]
        public void TestFormattingRemoved()
        {
            Init(users: 1);
            var text0 = Texts[Users[0]];

            text0.Insert(0, "ab", new Dictionary<string, object> { { "bold", true } });
            text0.Delete(0, 2);

            Assert.AreEqual(1, GetTypeChildren(text0));
        }

        [TestMethod]
        public void TestFormattingRemovedMidText()
        {
            Init(users: 1);
            var text0 = Texts[Users[0]];

            text0.Insert(0, "1234");
            text0.Insert(2, "ab", new Dictionary<string, object> { { "bold", true } });
            text0.Delete(2, 2);

            Assert.AreEqual(3, GetTypeChildren(text0));
        }

        [TestMethod]
        public void TestInsertAndDeleteAtRandomPositions()
        {
            const int N = 10_000;
            
            var rand = new Random();

            Init(users: 1);
            var text0 = Texts[Users[0]];

            // Create initial content.
            var expectedResult = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                var str = Guid.NewGuid().ToString();
                expectedResult.Insert(0, str);
                text0.Insert(0, str);
            }

            // Apply changes.
            for (int i = 0; i < N; i++)
            {
                var pos = rand.Next(0, text0.Length);

                if (rand.Next(2) == 1)
                {
                    var len = rand.Next(1, 5);
                    var word = new string(GetRandomChar(rand), len);
                    text0.Insert(pos, word);
                    expectedResult.Insert(pos, word);
                }
                else
                {
                    var len = rand.Next(0, Math.Min(3, text0.Length - pos));
                    text0.Delete(pos, len);
                    expectedResult.Remove(pos, len);
                }
            }

            Assert.AreEqual(expectedResult.ToString(), text0.ToString());
        }

        [TestMethod]
        public void TestAppendChars()
        {
            const int N = 10_000;

            Init(users: 1);
            var text0 = Texts[Users[0]];

            for (int i = 0; i < N; i++)
            {
                text0.Insert(text0.Length, "a");
            }

            Assert.AreEqual(N, text0.Length);
        }

        [TestMethod]
        public void TestSplitSurrogateCharacter()
        {
            {
                Init(users: 2);
                var text0 = Texts[Users[0]];

                // Disconnect forces the user to encoder the split surrogate.
                Users[1].Disconnect();

                // Insert surrogate character.
                text0.Insert(0, "👾");

                // Split surrogate, it should not lead to an encoding error.
                text0.Insert(1, "hi!");

                CompareUsers();
            }

            {
                Init(users: 2);
                var text0 = Texts[Users[0]];

                // Disconnect forces the user to encoder the split surrogate.
                Users[1].Disconnect();

                // Insert surrogate character.
                text0.Insert(0, "👾👾");

                // Partially delete surrogate.
                text0.Delete(1, 2);

                CompareUsers();
            }

            {
                Init(users: 2);
                var text0 = Texts[Users[0]];

                // Disconnect forces the user to encoder the split surrogate.
                Users[1].Disconnect();

                // Insert surrogate character.
                text0.Insert(0, "👾👾");

                // Formatting will also split surrogates.
                text0.Format(1, 2, new Dictionary<string, object> { { "bold", true } });

                CompareUsers();
            }
        }

        [DataTestMethod]
        [DataRow(5, 20)]
        [DataRow(5, 40)]
        [DataRow(5, 42)]
        [DataRow(5, 43)]
        [DataRow(5, 44)]
        [DataRow(5, 45)]
        [DataRow(5, 46)]
        [DataRow(5, 300)]
        [DataRow(5, 400)]
        [DataRow(5, 500)]
        [DataRow(5, 600)]
        [DataRow(5, 1_000)]
        [DataRow(5, 1_800)]
        /*
        [DataRow(5, 3_000)]
        [DataRow(5, 5_000)]
        [DataRow(5, 15_000)]
        */
        public void TestRepeatingGenerateTextChanges(int users, int iterations)
        {
            RandomTests(new List<Action<TestYInstance, Random>>
            {
                // Insert text
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var insertPos = rand.Next(0, ytext.Length);
                    var text = new string(GetRandomChar(rand), 2);

                    var prevText = ytext.ToString();
                    ytext.Insert(insertPos, text);
                    Assert.AreEqual(prevText.Substring(0, insertPos) + text + prevText.Substring(insertPos), ytext.ToString());
                },

                // Delete text
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var contentLen = ytext.Length;
                    var insertPos = rand.Next(0, contentLen);
                    var overwrite = Math.Min(rand.Next(0, contentLen - insertPos), contentLen);

                    var prevText = ytext.ToString();
                    ytext.Delete(insertPos, overwrite);
                    Assert.AreEqual(prevText.Substring(0, insertPos) + prevText.Substring(insertPos + overwrite), ytext.ToString());
                }
            }, users, iterations);
        }

        [DataTestMethod]
        [DataRow(5, 20)]
        /*
        [DataRow(5, 40)]
        [DataRow(5, 42)]
        [DataRow(5, 43)]
        [DataRow(5, 44)]
        [DataRow(5, 45)]
        [DataRow(5, 46)]
        [DataRow(5, 300)]
        [DataRow(5, 400)]
        [DataRow(5, 500)]
        [DataRow(5, 600)]
        [DataRow(5, 1_000)]
        [DataRow(5, 1_800)]
        /*
        [DataRow(5, 3_000)]
        [DataRow(5, 5_000)]
        [DataRow(5, 15_000)]
        */
        public void TestRandomYTextFeatures(int users, int iterations)
        {
            var attributes = new[]
            {
                null,
                new Dictionary<string, object> { { "bold", true } },
                new Dictionary<string, object> { { "italic", true } },
                new Dictionary<string, object> { { "italic", true }, { "color", "#888" } }
            };

            RandomTests(new List<Action<TestYInstance, Random>>
            {
                // Insert text
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var insertPos = rand.Next(0, ytext.Length);
                    var attrs = attributes[rand.Next(0, attributes.Length)];
                    var text = new string(GetRandomChar(rand), 2);
                    ytext.Insert(insertPos, text, attrs);
                },

                // Insert embed.
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var insertPos = rand.Next(0, ytext.Length);
                    ytext.InsertEmbed(insertPos, new[] { "video", "https://www.youtube.com/watch?v=dQw4w9WgXcQ" });
                },

                // Delete text
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var contentLen = ytext.Length;
                    var insertPos = rand.Next(0, contentLen);
                    var overwrite = Math.Min(rand.Next(0, contentLen - insertPos), contentLen);
                    ytext.Delete(insertPos, overwrite);
                },

                // Format text.
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var contentLen = ytext.Length;
                    var insertPos = rand.Next(0, contentLen);
                    var overwrite = Math.Min(rand.Next(0, contentLen - insertPos), contentLen);
                    var format = attributes[rand.Next(/* skip 'null' at 0 */ 1, attributes.Length)];
                    ytext.Format(insertPos, overwrite, format);
                },

                // Insert codeblock.
                (user, rand) =>
                {
                    var ytext = user.GetText("text");
                    var insertPos = rand.Next(0, ytext.Length);
                    var text = new string(GetRandomChar(rand), 2);

                    var ops = new List<Delta>();
                    if (insertPos > 0)
                    {
                        ops.Add(new Delta { Retain = insertPos });
                    }

                    ops.Add(new Delta
                    {
                        Insert = new List<object> { text }
                    });

                    ops.Add(new Delta
                    {
                        Insert = new List<object> { "\n" },
                        Attributes = new Dictionary<string, object> { { "code-block", true } }
                    });

                    ytext.ApplyDelta(ops);
                },
            }, users, iterations);
        }

        private static int GetTypeChildren(AbstractType type)
        {
            int result = 0;

            var s = type._start;
            while (s != null)
            {
                result++;
                s = s.Right as Item;
            }

            return result;
        }
    }
}
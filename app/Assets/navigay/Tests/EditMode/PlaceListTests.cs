using System;
using System.Collections.Generic;
using System.IO;
using Navigay.Data;
using Navigay.UI;
using NUnit.Framework;
using UnityEngine;

namespace Navigay.Tests.EditMode
{
    public sealed class PlaceListTests
    {
        [Test]
        public void LiveGroupsMatchPlacesCsvAndPutB2BeforeB10()
        {
            var graph = GraphLoader.LoadFile(Path.Combine(Application.streamingAssetsPath, "graph.json"));
            var csvPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "map", "places.csv"));
            var csv = ReadCsv(csvPath);
            Assert.AreEqual(csv.Count, graph.Places.Length);
            foreach (var place in graph.Places)
            {
                Assert.IsTrue(csv.ContainsKey(place.PlaceId), place.PlaceId);
                var row = csv[place.PlaceId];
                Assert.AreEqual(row.Name, place.Name);
                Assert.AreEqual(row.Type, place.Type);
                Assert.AreEqual(row.Access, place.Access);
                Assert.AreEqual(row.Start, place.Start);
                CollectionAssert.AreEqual(row.DoorIds, place.DoorIds);
            }

            var sections = PlaceList.Build(graph.Places, string.Empty, null, null, null);
            Assert.AreEqual(new[] { "Blocks", "Food", "Gates" }, Headings(sections));
            var blocks = sections[0].Places;
            var b2 = IndexOf(blocks, "B2");
            var b10 = IndexOf(blocks, "B10");
            Assert.GreaterOrEqual(b2, 0);
            Assert.Less(b2, b10);
            Assert.IsTrue(Array.Exists(blocks, place => place.Access == "boys"));
            Assert.IsTrue(Array.Exists(blocks, place => place.Access == "girls"));

            var mess = PlaceList.Build(graph.Places, "mess", null, null, null);
            Assert.AreEqual(1, mess.Length);
            Assert.IsNull(mess[0].Heading);
            Assert.AreEqual("BlueDove Mess", mess[0].Places[0].Name);
            Assert.AreEqual("Quess Mess", mess[0].Places[1].Name);

            var numbered = PlaceList.Build(graph.Places, "b1", null, null, null);
            Assert.IsNull(numbered[0].Heading);
            Assert.AreEqual("B1", numbered[0].Places[0].Name);
            Assert.AreEqual("B10", numbered[0].Places[1].Name);

            var access = PlaceList.Build(graph.Places, "boys", null, null, null);
            Assert.AreEqual(0, access[0].Places.Length);
        }

        [Test]
        public void TwoPlaceSliceOmitsEmptyGroups()
        {
            var sections = PlaceList.Build(new[]
            {
                Place("a", "North", PlaceKind.Block, "boys"),
                Place("b", "Cafe", PlaceKind.Food, "all"),
            }, string.Empty, null, null, null);

            Assert.AreEqual(2, sections.Length);
            Assert.AreEqual("Blocks", sections[0].Heading);
            Assert.AreEqual("North", sections[0].Places[0].Name);
            Assert.AreEqual("Food", sections[1].Heading);
            Assert.AreEqual("Cafe", sections[1].Places[0].Name);
        }

        [Test]
        public void StuckPointRanksOnceAndKeepsEveryAccess()
        {
            var places = new[]
            {
                Place("a", "B1", PlaceKind.Block, "boys"),
                Place("b", "B10", PlaceKind.Block, "girls"),
                Place("c", "Desk", PlaceKind.Office, "staff"),
                Place("d", "Gate", PlaceKind.Gate, "all"),
            };
            var metres = new Dictionary<string, double>
            {
                ["a"] = 50d,
                ["b"] = 10d,
                ["c"] = 30d,
            };
            var ranking = new StuckRanking();
            var first = PlaceList.Build(places, string.Empty, ranking, "stuck-a", metres);
            Assert.AreEqual(PlaceList.NearYouHeading, first[0].Heading);
            Assert.AreEqual(new[] { "B10", "Desk", "B1", "Gate" }, Names(first[0].Places));

            metres["b"] = 500d;
            var second = PlaceList.Build(places, "  ", ranking, "stuck-a", metres);
            Assert.AreEqual(new[] { "B10", "Desk", "B1", "Gate" }, Names(second[0].Places));

            var third = PlaceList.Build(places, string.Empty, ranking, "stuck-b", metres);
            Assert.AreEqual(new[] { "Desk", "B1", "B10", "Gate" }, Names(third[0].Places));

            var typed = PlaceList.Build(places, "de", ranking, "stuck-b", metres);
            Assert.IsNull(typed[0].Heading);
            Assert.AreEqual("Desk", typed[0].Places[0].Name);
        }

        static PlaceRecord Place(string id, string name, PlaceKind kind, string access)
        {
            return new PlaceRecord
            {
                PlaceId = id,
                Name = name,
                Type = kind.ToString().ToLowerInvariant(),
                Access = access,
                Kind = kind,
                DoorIds = Array.Empty<string>(),
            };
        }

        static Dictionary<string, PlaceRecord> ReadCsv(string path)
        {
            var rows = new Dictionary<string, PlaceRecord>();
            var lines = File.ReadAllLines(path);
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var fields = lines[i].Split(',');
                rows[fields[0]] = new PlaceRecord
                {
                    PlaceId = fields[0],
                    Name = fields[1],
                    Type = fields[2],
                    Access = fields[3],
                    DoorIds = string.IsNullOrEmpty(fields[4]) ? Array.Empty<string>() : new[] { fields[4] },
                    Start = fields[5] == "yes",
                };
            }

            return rows;
        }

        static string[] Headings(PlaceSection[] sections)
        {
            var headings = new string[sections.Length];
            for (var i = 0; i < sections.Length; i++)
            {
                headings[i] = sections[i].Heading;
            }

            return headings;
        }

        static string[] Names(PlaceRecord[] places)
        {
            var names = new string[places.Length];
            for (var i = 0; i < places.Length; i++)
            {
                names[i] = places[i].Name;
            }

            return names;
        }

        static int IndexOf(PlaceRecord[] places, string name)
        {
            for (var i = 0; i < places.Length; i++)
            {
                if (places[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

using System.IO;
using Navigay.Data;
using NUnit.Framework;
using UnityEngine;

namespace Navigay.Tests.EditMode
{
    public sealed class GraphLoaderTests
    {
        const string Shape = "{"
            + "\"schema\":1,"
            + "\"origin\":{\"lat\":1.25,\"lon\":-3.5},"
            + "\"metres_per_deg\":{\"x\":10.5,\"y\":20.25},"
            + "\"nodes\":[{\"id\":\"n1\",\"kind\":\"door\",\"x\":1.5,\"y\":2.5,\"lat\":1.25,\"lon\":-3.5,\"place_id\":\"p\",\"landmark\":null,\"edge_ids\":[\"e1\"]}],"
            + "\"edges\":[{\"id\":\"e1\",\"kind\":\"door\",\"from\":\"n1\",\"to\":\"n1\",\"lane_id\":null,\"access\":\"staff\",\"length_m\":4.5,\"polyline\":[[0,1],[2,3]]}],"
            + "\"places\":[{\"place_id\":\"p\",\"name\":\"North\",\"type\":\"block\",\"access\":\"all\",\"door_ids\":[\"d\"],\"start\":false}],"
            + "\"blocks\":[{\"id\":\"b\",\"place_id\":\"p\",\"name\":\"North\",\"rings\":[[[0,0],[1,0],[1,1]]]}],"
            + "\"photo_maps\":[{\"pmap_id\":\"m\",\"file\":\"m.bytes\",\"x\":1,\"y\":2,\"z\":3,\"yaw_deg\":4,\"load_radius_m\":5,\"junction_ids\":[\"n1\"]}],"
            + "\"extra\":true"
            + "}";

        [Test]
        public void ReadsSchemaFieldsInsteadOfHardCodedMetres()
        {
            var data = GraphLoader.Load(Shape);
            Assert.AreEqual(1, data.Schema);
            Assert.AreEqual(1.25d, data.Origin.Lat);
            Assert.AreEqual(-3.5d, data.Origin.Lon);
            Assert.AreEqual(10.5d, data.MetresPerDegree.X);
            Assert.AreEqual(20.25d, data.MetresPerDegree.Y);
            Assert.AreEqual(1.5d, data.Nodes[0].X);
            Assert.AreEqual("staff", data.Edges[0].Access);
            Assert.AreEqual(3d, data.Edges[0].Polyline[1][1]);
            Assert.AreEqual(PlaceKind.Block, data.Places[0].Kind);
            Assert.AreEqual("d", data.Places[0].DoorIds[0]);
            Assert.AreEqual(3, data.Blocks[0].Rings[0].Length);
            Assert.AreEqual("n1", data.PhotoMaps[0].JunctionIds[0]);
        }

        [Test]
        public void MissingOrBadFileStops()
        {
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load(null));
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load(""));
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load("{"));
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load("{\"schema\":2}"));
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load("{\"schema\":1,\"origin\":{\"lat\":0,\"lon\":0},\"metres_per_deg\":{\"x\":1,\"y\":1}}"));
            Assert.Throws<GraphLoadException>(() => GraphLoader.LoadFile(Path.Combine(Application.dataPath, "missing-graph.json")));

            var duplicate = Shape.Replace(
                "\"places\":[{\"place_id\":\"p\"",
                "\"places\":[{\"place_id\":\"p\",\"name\":\"Other\",\"type\":\"food\",\"access\":\"all\",\"door_ids\":[],\"start\":false},{\"place_id\":\"p\"");
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load(duplicate));
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load(Shape.Replace("\"type\":\"block\"", "\"type\":\"shop\"")));
            Assert.Throws<GraphLoadException>(() => GraphLoader.Load(Shape.Replace("\"access\":\"all\"", "\"access\":\"guest\"")));
        }
    }
}

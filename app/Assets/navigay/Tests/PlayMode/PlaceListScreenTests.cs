using System.Collections;
using System.IO;
using Navigay.Data;
using Navigay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Navigay.Tests.PlayMode
{
    public sealed class PlaceListScreenTests
    {
        [UnityTest]
        public IEnumerator ListShowsTheGraphAndOpensTheCard()
        {
            for (var frame = 0; frame < 30 && Object.FindAnyObjectByType<PlaceListScreen>() == null; frame++)
            {
                yield return null;
            }

            var graph = GraphLoader.LoadFile(Path.Combine(Application.streamingAssetsPath, "graph.json"));
            var texts = Labels();
            Assert.IsTrue(Has(texts, "Where to?"));
            Assert.IsTrue(Has(texts, "Blocks"));
            Assert.IsTrue(Has(texts, "Food"));
            Assert.IsTrue(Has(texts, "Gates"));
            Assert.IsFalse(Has(texts, "Offices and services"));
            Assert.IsFalse(Has(texts, "Sports"));
            Assert.IsFalse(Has(texts, PlaceList.NearYouHeading));
            Assert.IsFalse(Has(texts, "Boys"));
            Assert.IsFalse(Has(texts, "Girls"));
            Assert.IsFalse(Has(texts, "Warden office"));
            Assert.IsFalse(Has(texts, "Pool"));
            foreach (var place in graph.Places)
            {
                Assert.IsTrue(Has(texts, place.Name), place.Name);
            }

            var sample = graph.Places[0];
            Button row = null;
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null && label.text == sample.Name)
                {
                    row = button;
                    break;
                }
            }

            Assert.IsNotNull(row);
            row.onClick.Invoke();
            yield return null;

            texts = Labels();
            Assert.IsTrue(Has(texts, sample.Name));
            Assert.IsTrue(Has(texts, PlaceTypeWord.Word(sample.Kind)));
            Assert.IsTrue(Has(texts, PlaceCard.TimeUntilStuck));
            Assert.IsTrue(Has(texts, PlaceCard.NearestEntrance));
            Assert.IsTrue(Has(texts, "Go"));

            var input = Object.FindAnyObjectByType<TMP_InputField>();
            input.text = "mess";
            yield return null;
            texts = Labels();
            Assert.IsFalse(Has(texts, "Blocks"));
            Assert.IsTrue(Has(texts, "Quess Mess"));
            Assert.IsTrue(Has(texts, "BlueDove Mess"));
            Assert.IsFalse(Has(texts, "Underpass entrance"));

            Button close = null;
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null && label.text == "X")
                {
                    close = button;
                    break;
                }
            }

            Assert.IsNotNull(close);
            close.onClick.Invoke();
            yield return null;
            Assert.IsFalse(Has(Labels(), PlaceCard.TimeUntilStuck));

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var permissions = Object.FindAnyObjectByType<PermissionsScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(canvas);
            Assert.IsNotNull(permissions);
            Assert.AreSame(canvas.transform, permissions.transform.parent);
            permissions.Show(PermissionsScreen.PreciseSentence);
            yield return null;
            var permissionsRect = (RectTransform)permissions.transform;
            Assert.AreEqual(Vector2.zero, permissionsRect.anchorMin);
            Assert.AreEqual(Vector2.one, permissionsRect.anchorMax);
            var ready = Object.FindAnyObjectByType<ReadyScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(ready);
            Assert.AreEqual("safe", ready.transform.parent.name);
            permissions.Hide();
        }

        static TMP_Text[] Labels()
        {
            return Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        }

        static bool Has(TMP_Text[] texts, string value)
        {
            foreach (var text in texts)
            {
                if (text.text == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

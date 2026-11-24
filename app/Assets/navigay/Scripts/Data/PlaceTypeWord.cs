using System;

namespace Navigay.Data
{
    public static class PlaceTypeWord
    {
        public static readonly PlaceKind[] Groups =
        {
            PlaceKind.Block,
            PlaceKind.Food,
            PlaceKind.Office,
            PlaceKind.Gate,
            PlaceKind.Sports,
        };

        public static PlaceKind Parse(string type)
        {
            switch (type)
            {
                case "block":
                    return PlaceKind.Block;
                case "food":
                    return PlaceKind.Food;
                case "office":
                    return PlaceKind.Office;
                case "gate":
                    return PlaceKind.Gate;
                case "sports":
                    return PlaceKind.Sports;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string Word(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Block:
                    return "Block";
                case PlaceKind.Food:
                    return "Food";
                case PlaceKind.Office:
                    return "Office";
                case PlaceKind.Gate:
                    return "Gate";
                case PlaceKind.Sports:
                    return "Sports";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static string Heading(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Block:
                    return "Blocks";
                case PlaceKind.Food:
                    return "Food";
                case PlaceKind.Office:
                    return "Offices and services";
                case PlaceKind.Gate:
                    return "Gates";
                case PlaceKind.Sports:
                    return "Sports";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}

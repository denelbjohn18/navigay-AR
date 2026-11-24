namespace Navigay.Routing
{
    public sealed class RouteResult
    {
        public const string NoPathMessage = "Can't find a way there.";

        public readonly Route Route;
        public readonly string Failure;

        RouteResult(Route route, string failure)
        {
            Route = route;
            Failure = failure;
        }

        public bool Found => Route != null;

        public static RouteResult Ready(Route route)
        {
            return new RouteResult(route, null);
        }

        public static RouteResult NoPath()
        {
            return new RouteResult(null, NoPathMessage);
        }
    }
}

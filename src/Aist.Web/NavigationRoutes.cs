namespace Aist.Web;

internal static class NavigationRoutes
{
    public const string Projects = "/projects";
    public static string ProjectDetails(Guid id) => $"/projects/{id}";
}

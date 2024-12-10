using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class CustomDocumentOrderFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var orderedPaths = swaggerDoc.Paths
            .OrderBy(path => 
                path.Key.Contains("/api/Auth") ? 0 :
                path.Key.Contains("/api/Folder") ? 1 :
                path.Key.Contains("/api/Document") ? 2 : 
                3) 
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        
        swaggerDoc.Paths = new OpenApiPaths();
        foreach (var path in orderedPaths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }
}
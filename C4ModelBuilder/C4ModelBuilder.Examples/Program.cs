using C4ModelBuilder.Analyzer;
using C4ModelBuilder.Models;
using C4ModelBuilder.PlantUmlCreator;

var solutionFilePath = "C:\\Repos\\kassa\\Afisha.Tickets.All.sln";

//var ctx = await SolutionAnalyzer.Analyze(solutionFilePath);

var ctx = new PlantUmlC4ComponentDiagram(
    Components:
    [
        new C4Component("AccountController", "AccountController", null, null),
        new C4Component("WebUserContext", "WebUserContext", null, null),
        new C4Component("PaymentSystemProvider", "PaymentSystemProvider", null, null),
        new C4Component("CacheKeyBuilder", "CacheKeyBuilder", null, null),
        new C4Component("GetObjectWithLinksHandler", "GetObjectWithLinksHandler", null, null),
        new C4Component("MetadataObjectRepositoryResolver", "MetadataObjectRepositoryResolver", null, null),
        new C4Component("ClassID", "ClassID", null, null),
    ],
    Relations:
    [
        new C4Relation("AccountController", "WebUserContext"),
        new C4Relation("AccountController", "PaymentSystemProvider"),
        new C4Relation("PaymentSystemProvider", "CacheKeyBuilder"),
        new C4Relation("PaymentSystemProvider", "GetObjectWithLinksHandler"),
        new C4Relation("GetObjectWithLinksHandler", "MetadataObjectRepositoryResolver"),
        new C4Relation("GetObjectWithLinksHandler", "ClassID"),
    ]);

var diagram = PlantUmlGenerator.Generate(ctx);
Console.WriteLine(diagram);

await File.WriteAllTextAsync("C:\\Users\\a.mikryukov\\Desktop\\uml.puml", diagram);

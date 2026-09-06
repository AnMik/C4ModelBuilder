using C4ModelBuilder.Models;
using C4ModelBuilder.PlantUmlCreator;

/*
 * Строим демонстрационное MemberNode-дерево (без Roslyn, вручную):
 *
 * Root
 * └── AccountController.GetStoredPaymentCards(CancellationToken)
 *      ├── WebUserContext.GetAccount()
 *      └── PaymentSystemProvider.GetByPaymentType(PaymentType, CancellationToken)
 *           ├── CacheKeyBuilder.BuildCacheKey(string, string[])
 *           └── GetObjectWithLinksHandler.HandleAsync(...)
 *                ├── MetadataObjectRepositoryResolver.Resolve(short)
 *                └── ClassID.GetClass(Type)
 */

var root = new MemberNode("Root");

var accountController = new MemberNode("AccountController.GetStoredPaymentCards(CancellationToken)");

var getAccount = new MemberNode("WebUserContext.GetAccount()");
accountController.AddChild(getAccount);

var getByPaymentType = new MemberNode("PaymentSystemProvider.GetByPaymentType(PaymentType,CancellationToken)");
accountController.AddChild(getByPaymentType);

var buildCacheKey = new MemberNode("CacheKeyBuilder.BuildCacheKey(string,string[])");
getByPaymentType.AddChild(buildCacheKey);

var handleAsync = new MemberNode("GetObjectWithLinksHandler.HandleAsync(GetObjectWithLinks<TResult>,CancellationToken)");
getByPaymentType.AddChild(handleAsync);

var resolve = new MemberNode("MetadataObjectRepositoryResolver.Resolve(short)");
handleAsync.AddChild(resolve);

var getClass = new MemberNode("ClassID.GetClass(Type)");
handleAsync.AddChild(getClass);

root.AddChild(accountController);

var diagram = PlantUmlGenerator.Generate(root);
Console.WriteLine(diagram);

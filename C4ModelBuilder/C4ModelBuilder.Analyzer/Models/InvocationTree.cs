namespace C4ModelBuilder.Analyzer.Models;

internal sealed class InvocationTree(string nodeName, bool isC4Component)
{
    /// <summary>
    /// Сигнатура узла (например, "AccountController" или "AccountController.GetStoredPaymentCards")
    /// </summary>
    public string NodeName { get; } = nodeName;

    /// <summary>
    /// Признак того, что на узле проставлен атрибут <see cref="C4ModelBuilder.Models.Attributes.C4ComponentAttribute"/>.
    /// </summary>
    public bool IsC4Component { get; } = isC4Component;

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<InvocationTree> Invocations { get; } = [];

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddInvocation(InvocationTree invocationTree) => Invocations.Add(invocationTree);
}

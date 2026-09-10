namespace C4ModelBuilder.Analyzer.Models;

internal sealed class InvocationTree(string nodeName, bool isComponent = false)
{
    /// <summary>
    /// Сигнатура узла (например, "AccountController" или "AccountController.GetStoredPaymentCards")
    /// </summary>
    public string NodeName { get; } = nodeName;

    /// <summary>
    /// Признак того, что на узле проставлен атрибут <see cref="C4ModelBuilder.Models.Attributes.C4ComponentAttribute"/>.
    /// </summary>
    public bool IsComponent { get; } = isComponent;

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<InvocationTree> Children { get; } = [];

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(InvocationTree child) => Children.Add(child);
}

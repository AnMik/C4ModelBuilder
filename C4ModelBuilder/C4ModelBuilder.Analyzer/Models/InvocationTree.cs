namespace C4ModelBuilder.Analyzer.Models;

internal sealed class InvocationTree(string nodeName)
{
    /// <summary>
    /// Сигнатура узла (например, "AccountController" или "AccountController.GetStoredPaymentCards")
    /// </summary>
    public string NodeName { get; } = nodeName;

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<InvocationTree> Children { get; } = [];

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(InvocationTree child) => Children.Add(child);
}

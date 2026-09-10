namespace C4ModelBuilder.Analyzer.Models;

internal sealed class MemberNode(string name)
{
    /// <summary>
    /// Сигнатура узла (например, "AccountController" или "AccountController.GetStoredPaymentCards")
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<MemberNode> Children { get; } = [];

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(MemberNode child) => Children.Add(child);
}

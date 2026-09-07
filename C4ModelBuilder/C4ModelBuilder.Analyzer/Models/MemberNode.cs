namespace C4ModelBuilder.Analyzer.Models;

internal sealed class MemberNode(string methodSignature)
{
    /// <summary>
    /// Сигнатура метода (например, "AccountController.GetStoredPaymentCards(CancellationToken)")
    /// </summary>
    public string MethodSignature { get; } = methodSignature;

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<MemberNode> Children { get; } = [];

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(MemberNode child) => Children.Add(child);
}

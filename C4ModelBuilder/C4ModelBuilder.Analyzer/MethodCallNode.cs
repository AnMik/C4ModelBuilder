namespace C4ModelBuilder.Analyzer;

internal sealed class MemberNode(string methodSignature, bool isEmpty = false)
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
    /// Признак того, что у метода нет значимых вызовов (например, тело пустое)
    /// </summary>
    public bool IsEmpty { get; set; } = isEmpty;

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(MemberNode child) => Children.Add(child);
}

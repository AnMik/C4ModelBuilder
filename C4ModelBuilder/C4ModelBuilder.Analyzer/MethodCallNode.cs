namespace C4ModelBuilder.Analyzer;

internal sealed class MemberNode
{
    /// <summary>
    /// Сигнатура метода (например, "AccountController.GetStoredPaymentCards(CancellationToken)")
    /// </summary>
    public string MethodSignature { get; set; }

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<MemberNode> Children { get; set; }

    /// <summary>
    /// Признак того, что у метода нет значимых вызовов (например, тело пустое)
    /// </summary>
    public bool IsEmpty { get; set; }

    public MemberNode(string methodSignature, bool isEmpty = false)
    {
        MethodSignature = methodSignature;
        IsEmpty = isEmpty;
        Children = new List<MemberNode>();
    }

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(MemberNode child)
    {
        Children.Add(child);
    }
}

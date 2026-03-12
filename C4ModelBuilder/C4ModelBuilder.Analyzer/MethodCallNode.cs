namespace C4ModelBuilder.Analyzer;

public class MethodCallNode
{
    /// <summary>
    /// Сигнатура метода (например, "AccountController.GetStoredPaymentCards(CancellationToken)")
    /// </summary>
    public string MethodSignature { get; set; }

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода
    /// </summary>
    public List<MethodCallNode> Children { get; set; }

    /// <summary>
    /// Признак того, что у метода нет значимых вызовов (например, тело пустое)
    /// </summary>
    public bool IsEmpty { get; set; }

    public MethodCallNode(string methodSignature, bool isEmpty = false)
    {
        MethodSignature = methodSignature;
        IsEmpty = isEmpty;
        Children = new List<MethodCallNode>();
    }

    /// <summary>
    /// Добавить дочерний узел
    /// </summary>
    public void AddChild(MethodCallNode child)
    {
        Children.Add(child);
    }
}

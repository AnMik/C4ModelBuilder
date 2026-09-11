namespace C4ModelBuilder.Models.Analysis;

/// <summary>
/// Дерево вызовов для одного корневого компонента (результат анализа одного root-класса).
/// Служит промежуточным представлением между анализом кода и построением диаграммы.
/// </summary>
public sealed class InvocationTree(string nodeName, string? c4ComponentDescription)
{
    // public sealed class C4Component(string description);

    /// <summary>
    /// Сигнатура узла (например, "AccountController" или "AdminController.SendPromoAsync").
    /// </summary>
    public string NodeName { get; } = nodeName;

    /// <summary>
    /// Данные атрибута, если на узле проставлен атрибут <see cref="C4ModelBuilder.Models.Attributes.C4ComponentAttribute"/>.
    /// </summary>
    public string? C4ComponentDescription { get; } = c4ComponentDescription;

    /// <summary>
    /// Дочерние узлы — вызовы методов из текущего метода.
    /// </summary>
    public IReadOnlyList<InvocationTree> Invocations { get; } = new List<InvocationTree>();

    /// <summary>
    /// Добавить дочерний узел.
    /// </summary>
    public void AddInvocation(InvocationTree invocationTree) => ((List<InvocationTree>)Invocations).Add(invocationTree);
}

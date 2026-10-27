namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>Como o avulso está inscrito no órgão gestor de mão de obra (Lei 12.815/2013, art. 41).</summary>
public enum Inscricao
{
    /// <summary>No cadastro: fez o treinamento, e completa as vagas que os registrados não preenchem.</summary>
    Cadastrado,

    /// <summary>No registro: tem a preferência na escala. O cadastrado passa a registrado quando abre vaga, pela ordem do cadastro.</summary>
    Registrado,
}

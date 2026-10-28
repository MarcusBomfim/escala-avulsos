using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Arquitetura.Testes;

public sealed class IdentidadeDosAgregadosTests
{
    [Fact]
    public void Todo_agregado_tem_identidade_propria()
    {
        /*
         * Raiz de agregado é o que a persistência vai gravar e a API endereçar:
         * tem um Id do seu próprio tipo de identificador, e não um Guid solto.
         */
        Type[] raizes = [typeof(Trabalhador), typeof(Requisicao), typeof(Escalacao), typeof(Escala)];

        var semIdentidade = raizes
            .Where(raiz => raiz.GetProperty("Id")?.PropertyType is not { } tipo
                || !tipo.IsSubclassOf(typeof(Identificador))
                || !tipo.Name.StartsWith(raiz.Name, StringComparison.Ordinal))
            .Select(raiz => raiz.Name);

        Assert.Empty(semIdentidade);
    }
}

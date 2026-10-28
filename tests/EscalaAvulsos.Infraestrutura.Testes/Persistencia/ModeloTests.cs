using EscalaAvulsos.Infraestrutura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

public sealed class ModeloTests
{
    [Fact]
    public void Toda_mudanca_no_modelo_tem_migracao()
    {
        /*
         * Mudou o mapeamento e esqueceu de gerar a migração: o código espera
         * uma coluna que o banco não tem. O EF compara o modelo com o
         * instantâneo da última migração — sem abrir conexão — e este teste
         * falha antes de alguém descobrir em produção.
         */
        using var contexto = new FabricaDoContextoEmTempoDeDesenho().CreateDbContext([]);

        Assert.False(contexto.Database.HasPendingModelChanges(), "Há mudança no modelo sem migração: rode dotnet ef migrations add.");
    }

    [Fact]
    public void O_esquema_tem_as_tabelas_dos_agregados_das_vagas_e_dos_rodizios()
    {
        using var contexto = new FabricaDoContextoEmTempoDeDesenho().CreateDbContext([]);

        var tabelas = contexto.Model.GetEntityTypes().Select(entidade => $"{entidade.GetSchema()}.{entidade.GetTableName()}").Order(StringComparer.Ordinal);

        Assert.Equal(
            ["escala.engajamentos", "escala.escalacoes", "escala.escalas", "escala.requisicoes", "escala.rodizios", "escala.trabalhadores"],
            tabelas);
    }
}

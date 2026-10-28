using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

// Um PostgreSQL para o assembly de testes inteiro: sobe uma vez, antes do
// primeiro teste que o pede.
[assembly: AssemblyFixture(typeof(BancoDeTestes))]

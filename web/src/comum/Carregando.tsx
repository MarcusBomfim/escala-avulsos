export function Carregando({ rotulo }: { readonly rotulo: string }) {
  return (
    <p className="carregando" role="status">
      {rotulo}…
    </p>
  );
}

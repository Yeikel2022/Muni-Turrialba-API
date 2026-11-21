namespace MuniTurrialbaAPI.Repositories
{
    public interface IHistoricoRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        void CrearHistorico(string nombreUsuarioParametrizado, string correoParametrizado, string accionParametrizado, string DescripcionParametrizado);
    }
}

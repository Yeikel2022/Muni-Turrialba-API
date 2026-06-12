using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IFAQRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que, cuando se usen, -
         * estos puedan aceptar valores nulos. */
        Task<bool?> CrearFAQ(FAQCreateDto faqdto, int idUsuarioParametrizado);
        Task<bool?> ActualizarFAQ(FAQCreateDto faqParametrizado, int idUsuarioParametrizado, string preguntaParametrizado);
        Task<bool?> EliminarFAQ(string preguntaParametrizada, int idUsuarioParametrizado);


        Task<IEnumerable<FAQEntitie>?> ObtenerFAQs();
        bool? ObtenerFAQ_PorPregunta(string preguntaParametrizada);
        bool? VerificarPregunta_ParaActualizar(string preguntaParametrizado, int idUsuarioParametrizado);
        bool? VerificarPregunta_ParaEliminar(string preguntaParametrizada, int idUsuarioParametrizado);


    }
}

using MuniTurrialbaAPI.Entities;
using MuniTurrialbaAPI.Models;

namespace MuniTurrialbaAPI.Repositories
{
    public interface IFAQRepository
    {
        /* |===========| Son los principales métodos que realizara el API |===========| 
           |===========|              pero solo es su mención.            |===========| */


        /* NOTA: Se le coloco el: " ? " en ciertos métodos para que cuando se usen -
         * puedan aceptar valores nulos. */
        Task<bool?> CrearFAQ(FAQCreateDto faqDto, int idUsuarioParametrizado);
        Task<bool?> ActualizarFAQ(FAQCreateDto faqDto, string preguntaParametrizada, int idUsuarioParametrizado);
        Task<bool?> EliminarFAQ(string preguntaParametrizada, int idUsuarioParametrizado);


        Task<IEnumerable<FAQEntitie>?> ObtenerFAQs();


        bool? VerificarPregunta_ParaCrear(string preguntaParametrizada);
        bool? VerificarPregunta_ParaActualizar(string preguntaParametrizada, int idUsuarioParametrizado);
        bool? VerificarPregunta_ParaEliminar(string preguntaParametrizada, int idUsuarioParametrizado);
    }
}

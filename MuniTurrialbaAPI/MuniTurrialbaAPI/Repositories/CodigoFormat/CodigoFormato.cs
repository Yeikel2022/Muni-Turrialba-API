using System.Text;

namespace MuniTurrialbaAPI.Repositories.CodigoFormat
{
    public class CodigoFormato
    {

        public string CodigoGenerado(int longitudParametrizada)
        {
            const string cadenaCaracteres = "WfjndFin*MBfu*HAdPNe$GTm(L5cf{Lc!94SLCZG+:7bY&U2+PnM(ER9Nurk.bZ&6xxzcw:A!*W?-xnA:]%u=yLmCSApY2K[5K##";
            
            StringBuilder constructorString = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < longitudParametrizada; i++)
            {
                int index = random.Next(cadenaCaracteres.Length);
                constructorString.Append(cadenaCaracteres[index]);
            }

            return constructorString.ToString();
        }

    }
}

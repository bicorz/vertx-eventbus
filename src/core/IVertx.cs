using System.Threading.Tasks;

namespace io.vertx.core
{

    public interface IVertx
    {
        Task Close();
    }

    public class Vertx
    {

        public static IVertx NewInstance()
        {
            return null;
        }

    }
}

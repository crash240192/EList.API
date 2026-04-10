using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.DbDataProvider.Converters
{
    public interface IDtoConverter
    {
        T FromDto<T>(T item);
    }
}

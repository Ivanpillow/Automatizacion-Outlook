using CorreosOutlook.Application;
using Scriban;
using Scriban.Runtime;

namespace CorreosOutlook.Infrastructure;

public sealed class MotorScriban : ITemplateEngine
{
    public string Render(string texto, IReadOnlyDictionary<string, object?> variables)
    {
        var plantilla = Template.Parse(texto);
        if (plantilla.HasErrors) throw new ValidacionException("La plantilla tiene sintaxis inválida. Revise sus bloques y variables.");
        var datos = new ScriptObject();
        foreach (var (key, value) in variables) datos.SetValue(key, value, true);
        var context = new TemplateContext { StrictVariables = true, LoopLimit = 1000, RecursiveLimit = 20 };
        context.PushGlobal(datos);
        try { return plantilla.Render(context); }
        catch (Scriban.Syntax.ScriptRuntimeException)
        {
            throw new ValidacionException("No se pudo completar la plantilla: variable desconocida o expresión inválida.");
        }
    }
}

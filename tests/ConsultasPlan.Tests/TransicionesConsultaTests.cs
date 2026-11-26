using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Tests;

public class TransicionesConsultaTests
{
    private static readonly DateTime Ahora = new(2026, 11, 26, 10, 0, 0);

    private static Consulta ConsultaEn(EstadoConsulta estado, DateTime fechaHora) =>
        new() { Id = 1, Estado = estado, FechaHora = fechaHora };

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Confirmado)]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Cancelado)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Cancelado)]
    public void Permite_transiciones_validas_sobre_consulta_futura(EstadoConsulta actual, EstadoConsulta nuevo)
    {
        var consulta = ConsultaEn(actual, Ahora.AddDays(3));

        Assert.Null(TransicionesConsulta.Validar(consulta, nuevo, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Inasistencia)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Inasistencia)]
    public void Permite_registrar_resultado_de_consulta_pasada(EstadoConsulta actual, EstadoConsulta nuevo)
    {
        var consulta = ConsultaEn(actual, Ahora.AddHours(-2));

        Assert.Null(TransicionesConsulta.Validar(consulta, nuevo, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Cancelado, EstadoConsulta.Pendiente)]
    [InlineData(EstadoConsulta.Cancelado, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Atendido, EstadoConsulta.Cancelado)]
    [InlineData(EstadoConsulta.Atendido, EstadoConsulta.Inasistencia)]
    [InlineData(EstadoConsulta.Inasistencia, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Pendiente)]
    public void Rechaza_transiciones_desde_estados_finales_o_hacia_atras(EstadoConsulta actual, EstadoConsulta nuevo)
    {
        var consulta = ConsultaEn(actual, Ahora.AddDays(-1));

        Assert.NotNull(TransicionesConsulta.Validar(consulta, nuevo, Ahora));
    }

    [Fact]
    public void Rechaza_pasar_al_mismo_estado()
    {
        var consulta = ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddDays(3));

        Assert.NotNull(TransicionesConsulta.Validar(consulta, EstadoConsulta.Pendiente, Ahora));
    }

    [Fact]
    public void Rechaza_cancelar_con_menos_de_24_horas()
    {
        var consulta = ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddHours(23).AddMinutes(30));

        var error = TransicionesConsulta.Validar(consulta, EstadoConsulta.Cancelado, Ahora);

        Assert.Equal("No se puede cancelar con menos de 24 horas de anticipación.", error);
    }

    [Fact]
    public void Permite_cancelar_con_exactamente_24_horas()
    {
        var consulta = ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddHours(24));

        Assert.Null(TransicionesConsulta.Validar(consulta, EstadoConsulta.Cancelado, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Inasistencia)]
    public void Rechaza_registrar_resultado_de_consulta_futura(EstadoConsulta nuevo)
    {
        var consulta = ConsultaEn(EstadoConsulta.Confirmado, Ahora.AddHours(1));

        Assert.NotNull(TransicionesConsulta.Validar(consulta, nuevo, Ahora));
    }
}

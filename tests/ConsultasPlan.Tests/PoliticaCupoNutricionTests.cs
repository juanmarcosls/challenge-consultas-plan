using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Tests;

/// <summary>
/// Casos basados en los afiliados del seed (DbSeeder), con "hoy" fijo en 26/11/2026.
/// </summary>
public class PoliticaCupoNutricionTests
{
    private const int Y = 2026;

    private static readonly Profesional Nutricionista = new() { Id = 4, NombreCompleto = "Lic. Julieta Arana", Especialidad = "Nutrición" };
    private static readonly Profesional Cardiologo = new() { Id = 3, NombreCompleto = "Dr. Carlos Benítez", Especialidad = "Cardiología" };

    private static Consulta C(DateTime fecha, EstadoConsulta estado,
        ModalidadConsulta modalidad = ModalidadConsulta.Presencial, Profesional? profesional = null)
    {
        var p = profesional ?? Nutricionista;
        return new Consulta { FechaHora = fecha, Estado = estado, Modalidad = modalidad, Profesional = p, ProfesionalId = p.Id };
    }

    private static List<Consulta> Mensuales(int desde, int hasta) =>
        Enumerable.Range(desde, hasta - desde + 1)
            .Select(m => C(new DateTime(Y, m, 10, 11, 0, 0), EstadoConsulta.Atendido))
            .ToList();

    // --- Casos del seed ---

    [Fact]
    public void Afiliado_sin_historial_puede_agendar()
    {
        Assert.Null(PoliticaCupoNutricion.Validar(new List<Consulta>(), new DateTime(Y, 12, 5, 10, 0, 0)));
    }

    [Fact]
    public void Con_5_consultas_en_el_anio_puede_agendar_la_sexta_en_un_mes_libre()
    {
        var consultas = Mensuales(6, 10); // junio a octubre

        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 12, 5, 10, 0, 0)));
    }

    [Fact]
    public void Con_6_consultas_en_el_anio_se_rechaza_por_cupo_anual()
    {
        var consultas = Mensuales(1, 6); // enero a junio

        var error = PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 12, 5, 10, 0, 0));

        Assert.NotNull(error);
        Assert.Contains("6 consultas", error);
    }

    [Fact]
    public void Con_6_consultas_puede_volver_a_agendar_el_anio_siguiente_porque_no_se_acumulan()
    {
        var consultas = Mensuales(1, 6);

        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y + 1, 1, 15, 10, 0, 0)));
    }

    [Fact]
    public void Con_una_consulta_en_el_mes_se_rechaza_otra_en_el_mismo_mes()
    {
        var consultas = new List<Consulta> { C(new DateTime(Y, 11, 1, 10, 0, 0), EstadoConsulta.Atendido) };

        var error = PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 11, 28, 10, 0, 0));

        Assert.NotNull(error);
        Assert.Contains("por mes", error);
    }

    [Fact]
    public void Con_una_consulta_en_el_mes_puede_agendar_el_mes_siguiente()
    {
        var consultas = new List<Consulta> { C(new DateTime(Y, 11, 1, 10, 0, 0), EstadoConsulta.Atendido) };

        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 12, 3, 10, 0, 0)));
    }

    [Fact]
    public void Consultas_virtuales_y_canceladas_no_consumen_cupo()
    {
        var consultas = new List<Consulta>
        {
            C(new DateTime(Y, 10, 15, 16, 0, 0), EstadoConsulta.Atendido, ModalidadConsulta.Virtual),
            C(new DateTime(Y, 9, 15, 16, 0, 0), EstadoConsulta.Cancelado)
        };

        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 10, 28, 10, 0, 0)));
        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 9, 28, 10, 0, 0)));
    }

    [Fact]
    public void Inasistencia_y_consulta_pendiente_futura_consumen_cupo()
    {
        var consultas = new List<Consulta>
        {
            C(new DateTime(Y, 10, 20, 9, 0, 0), EstadoConsulta.Inasistencia),
            C(new DateTime(Y, 12, 16, 15, 0, 0), EstadoConsulta.Pendiente)
        };

        Assert.NotNull(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 10, 27, 10, 0, 0)));
        Assert.NotNull(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 12, 2, 10, 0, 0)));
        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 11, 30, 10, 0, 0)));
    }

    [Fact]
    public void Consulta_del_anio_anterior_no_cuenta_para_el_anio_en_curso()
    {
        var consultas = new List<Consulta>
        {
            C(new DateTime(Y - 1, 12, 15, 11, 0, 0), EstadoConsulta.Atendido),
            C(new DateTime(Y, 3, 8, 11, 0, 0), EstadoConsulta.Atendido, profesional: Cardiologo)
        };

        // Diciembre del año en curso: mismo mes que la del año anterior, pero otro año.
        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 12, 15, 11, 0, 0)));
        Assert.Equal(0, PoliticaCupoNutricion.Resumir(8, consultas, Y).Usadas);
    }

    // --- Alcance de la política ---

    [Theory]
    [InlineData("Nutrición", ModalidadConsulta.Presencial, true)]
    [InlineData("nutricion", ModalidadConsulta.Presencial, true)]
    [InlineData(" NUTRICIÓN ", ModalidadConsulta.Presencial, true)]
    [InlineData("Nutrición", ModalidadConsulta.Virtual, false)]
    [InlineData("Cardiología", ModalidadConsulta.Presencial, false)]
    public void Aplica_solo_a_nutricion_presencial(string especialidad, ModalidadConsulta modalidad, bool esperado)
    {
        var profesional = new Profesional { Especialidad = especialidad };

        Assert.Equal(esperado, PoliticaCupoNutricion.Aplica(profesional, modalidad));
    }

    [Fact]
    public void Consultas_de_otras_especialidades_no_consumen_cupo()
    {
        var consultas = Enumerable.Range(1, 11)
            .Select(m => C(new DateTime(Y, m, 10, 10, 0, 0), EstadoConsulta.Atendido, profesional: Cardiologo))
            .ToList();

        Assert.Null(PoliticaCupoNutricion.Validar(consultas, new DateTime(Y, 11, 20, 10, 0, 0)));
    }

    [Fact]
    public void Resumen_informa_usadas_disponibles_y_meses()
    {
        var consultas = Mensuales(6, 10);
        consultas.Add(C(new DateTime(Y, 1, 10, 10, 0, 0), EstadoConsulta.Cancelado));

        var resumen = PoliticaCupoNutricion.Resumir(2, consultas, Y);

        Assert.Equal(5, resumen.Usadas);
        Assert.Equal(1, resumen.Disponibles);
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, resumen.MesesUtilizados);
    }
}

<template>
  <div class="card" style="max-width: 560px">
    <h2>Nueva consulta</h2>
    <form @submit.prevent="guardar">
      <div class="form-group">
        <label>Afiliado</label>
        <select v-model="form.afiliadoId" required>
          <option value="">Seleccioná un afiliado</option>
          <option v-for="a in afiliados" :key="a.id" :value="a.id" :disabled="a.bloqueado">
            {{ a.nombreCompleto }} — DNI {{ a.dni }}{{ a.bloqueado ? ' (bloqueado)' : '' }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Profesional</label>
        <select v-model="form.profesionalId" required>
          <option value="">Seleccioná un profesional</option>
          <option v-for="p in profesionales" :key="p.id" :value="p.id">
            {{ p.nombreCompleto }} — {{ p.especialidad }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Modalidad</label>
        <select v-model="form.modalidad">
          <option value="Presencial">Presencial</option>
          <option value="Virtual">Virtual</option>
        </select>
      </div>
      <div class="form-group">
        <label>Fecha y hora</label>
        <input type="datetime-local" v-model="form.fechaHora" :min="fechaMinima" required />
      </div>
      <div class="form-group">
        <label>Motivo</label>
        <input type="text" v-model="form.motivo" maxlength="200" placeholder="Motivo de la consulta" />
      </div>

      <div v-if="aplicaCupo && cupo" :class="['cupo', { 'cupo-agotado': motivoSinCupo }]">
        <strong>Cupo de nutrición presencial {{ cupo.anio }}:</strong>
        {{ cupo.usadas }} de {{ cupo.cupoAnual }} usadas ({{ cupo.disponibles }} disponibles).
        <span v-if="cupo.mesesUtilizados.length">
          Meses ya utilizados: {{ cupo.mesesUtilizados.map(m => MESES[m - 1]).join(', ') }}.
        </span>
        <div v-if="motivoSinCupo" style="margin-top: 6px; font-weight: 600">{{ motivoSinCupo }}</div>
      </div>
      <div v-else-if="esNutricionVirtual" class="cupo">
        Las consultas virtuales de nutrición no consumen cupo.
      </div>

      <p v-if="error" class="error" style="margin-bottom: 16px">{{ error }}</p>
      <button type="submit" :disabled="guardando || !!motivoSinCupo">
        {{ guardando ? 'Guardando...' : 'Confirmar consulta' }}
      </button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi, mensajeDeError } from '../services/api'
import { ahoraParaInput, esNutricion, MESES } from '../utils/consultas'

export default {
  name: 'ConsultaNueva',
  data() {
    return {
      form: {
        afiliadoId: '',
        profesionalId: '',
        modalidad: 'Presencial',
        fechaHora: '',
        motivo: ''
      },
      afiliados: [],
      profesionales: [],
      fechaMinima: ahoraParaInput(),
      error: null,
      guardando: false,
      cupo: null,
      MESES
    }
  },
  computed: {
    profesionalSeleccionado() {
      return this.profesionales.find(p => p.id === Number(this.form.profesionalId))
    },
    esConsultaNutricion() {
      return esNutricion(this.profesionalSeleccionado?.especialidad)
    },
    aplicaCupo() {
      return this.esConsultaNutricion && this.form.modalidad === 'Presencial' && !!this.form.afiliadoId
    },
    esNutricionVirtual() {
      return this.esConsultaNutricion && this.form.modalidad === 'Virtual'
    },
    anioConsulta() {
      return this.form.fechaHora ? Number(this.form.fechaHora.slice(0, 4)) : new Date().getFullYear()
    },
    mesConsulta() {
      return this.form.fechaHora ? Number(this.form.fechaHora.slice(5, 7)) : null
    },
    /** Aviso anticipado; la validación definitiva la hace el backend. */
    motivoSinCupo() {
      if (!this.aplicaCupo || !this.cupo) return null
      if (this.cupo.disponibles === 0)
        return `El afiliado ya utilizó las ${this.cupo.cupoAnual} consultas del año ${this.cupo.anio}.`
      if (this.mesConsulta && this.cupo.mesesUtilizados.includes(this.mesConsulta))
        return `El afiliado ya tiene una consulta de nutrición presencial en ${MESES[this.mesConsulta - 1]}.`
      return null
    }
  },
  watch: {
    aplicaCupo: 'cargarCupo',
    anioConsulta: 'cargarCupo',
    'form.afiliadoId': 'cargarCupo'
  },
  async mounted() {
    try {
      const [aRes, pRes] = await Promise.all([afiliadosApi.getAll(), profesionalesApi.getAll()])
      this.afiliados = aRes.data
      this.profesionales = pRes.data
    } catch (e) {
      this.error = mensajeDeError(e)
    }
  },
  methods: {
    async cargarCupo() {
      this.cupo = null
      if (!this.aplicaCupo) return
      const afiliadoId = Number(this.form.afiliadoId)
      const anio = this.anioConsulta
      try {
        const res = await afiliadosApi.getCupoNutricion(afiliadoId, anio)
        // Evita pisar el dato si el usuario cambió la selección mientras se cargaba.
        if (afiliadoId === Number(this.form.afiliadoId) && anio === this.anioConsulta) this.cupo = res.data
      } catch {
        this.cupo = null
      }
    },
    async guardar() {
      this.error = null
      this.guardando = true
      try {
        await consultasApi.create({
          afiliadoId: Number(this.form.afiliadoId),
          profesionalId: Number(this.form.profesionalId),
          modalidad: this.form.modalidad,
          fechaHora: this.form.fechaHora,
          motivo: this.form.motivo
        })
        this.$router.push('/consultas')
      } catch (e) {
        this.error = mensajeDeError(e)
      } finally {
        this.guardando = false
      }
    }
  }
}
</script>

<style scoped>
.cupo {
  background: #e8f0fe;
  color: #1a3e72;
  border-radius: 4px;
  padding: 10px 12px;
  font-size: 13px;
  margin-bottom: 16px;
}
.cupo-agotado {
  background: #fdecea;
  color: #8a1c1c;
}
</style>

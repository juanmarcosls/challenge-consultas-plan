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
      <p v-if="error" class="error" style="margin-bottom: 16px">{{ error }}</p>
      <button type="submit" :disabled="guardando">
        {{ guardando ? 'Guardando...' : 'Confirmar consulta' }}
      </button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi, mensajeDeError } from '../services/api'
import { ahoraParaInput } from '../utils/consultas'

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
      guardando: false
    }
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

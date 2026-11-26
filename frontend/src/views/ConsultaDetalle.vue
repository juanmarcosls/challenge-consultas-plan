<template>
  <div>
    <router-link to="/consultas" style="font-size:14px; color:#1a73e8">← Volver a consultas</router-link>
    <div v-if="consulta" class="card" style="margin-top: 20px; max-width: 560px">
      <h2>Consulta #{{ consulta.id }}</h2>
      <div class="detail-row"><span class="label">Afiliado</span><span>{{ consulta.afiliado?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">DNI</span><span>{{ consulta.afiliado?.dni }}</span></div>
      <div class="detail-row"><span class="label">Profesional</span><span>{{ consulta.profesional?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">Especialidad</span><span>{{ consulta.profesional?.especialidad }}</span></div>
      <div class="detail-row"><span class="label">Fecha y hora</span><span>{{ formatFecha(consulta.fechaHora) }}</span></div>
      <div class="detail-row"><span class="label">Modalidad</span><span>{{ consulta.modalidad }}</span></div>
      <div class="detail-row"><span class="label">Estado</span><span>{{ consulta.estado }}</span></div>
      <div class="detail-row"><span class="label">Motivo</span><span>{{ consulta.motivo }}</span></div>

      <div v-if="esActiva(consulta)" style="display: flex; gap: 10px; margin-top: 24px; flex-wrap: wrap">
        <button v-if="puedeConfirmar(consulta)" :disabled="procesando" @click="ejecutar('confirmar')">Confirmar</button>
        <button :disabled="procesando" @click="ejecutar('registrarAtencion')">Registrar atención</button>
        <button :disabled="procesando" @click="ejecutar('marcarInasistencia')">Marcar inasistencia</button>
        <button class="btn-danger" :disabled="procesando" @click="cancelar">Cancelar consulta</button>
      </div>
      <p v-else class="estado-final">La consulta está en un estado final y no admite más acciones.</p>
    </div>
    <p v-else-if="error" class="error" style="margin-top: 20px">{{ error }}</p>
    <p v-else style="margin-top: 20px">Cargando...</p>
  </div>
</template>

<script>
import { consultasApi, mensajeDeError } from '../services/api'
import { esActiva, puedeConfirmar, formatFecha } from '../utils/consultas'

export default {
  name: 'ConsultaDetalle',
  data() {
    return {
      consulta: null,
      error: null,
      procesando: false
    }
  },
  async mounted() {
    try {
      const res = await consultasApi.getById(this.$route.params.id)
      this.consulta = res.data
    } catch (e) {
      this.error = mensajeDeError(e)
    }
  },
  methods: {
    esActiva,
    puedeConfirmar,
    formatFecha,
    async ejecutar(accion) {
      this.procesando = true
      try {
        const res = await consultasApi[accion](this.consulta.id)
        this.consulta = res.data
      } catch (e) {
        alert(mensajeDeError(e))
      } finally {
        this.procesando = false
      }
    },
    async cancelar() {
      if (!confirm('¿Cancelar esta consulta?')) return
      await this.ejecutar('cancelar')
    }
  }
}
</script>

<style scoped>
.detail-row {
  display: flex;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;
}
.label {
  width: 130px;
  font-weight: 600;
  color: #666;
  flex-shrink: 0;
}
.estado-final {
  margin-top: 24px;
  font-size: 13px;
  color: #777;
}
</style>

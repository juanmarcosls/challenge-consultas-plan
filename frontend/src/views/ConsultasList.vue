<template>
  <div>
    <h2>Consultas</h2>
    <p v-if="cargando">Cargando...</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <table v-else-if="consultas.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Afiliado</th>
          <th>Profesional</th>
          <th>Especialidad</th>
          <th>Fecha y hora</th>
          <th>Modalidad</th>
          <th>Estado</th>
          <th>Motivo</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="consulta in consultas" :key="consulta.id">
          <td>{{ consulta.id }}</td>
          <td>{{ consulta.afiliado?.nombreCompleto }}</td>
          <td>{{ consulta.profesional?.nombreCompleto }}</td>
          <td>{{ consulta.profesional?.especialidad }}</td>
          <td>{{ formatFecha(consulta.fechaHora) }}</td>
          <td>{{ consulta.modalidad }}</td>
          <td>
            <span :class="['badge', `badge-${consulta.estado?.toLowerCase()}`]">{{ consulta.estado }}</span>
          </td>
          <td>{{ consulta.motivo }}</td>
          <td class="acciones">
            <router-link :to="`/consultas/${consulta.id}`">Ver</router-link>
            <button
              v-if="esActiva(consulta)"
              class="btn-danger"
              style="margin-left: 8px"
              :disabled="procesando === consulta.id"
              @click="cancelar(consulta)"
            >Cancelar</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay consultas registradas.</p>
  </div>
</template>

<script>
import { consultasApi, mensajeDeError } from '../services/api'
import { esActiva, formatFecha } from '../utils/consultas'

export default {
  name: 'ConsultasList',
  data() {
    return {
      consultas: [],
      cargando: true,
      error: null,
      procesando: null
    }
  },
  async mounted() {
    try {
      const res = await consultasApi.getAll()
      this.consultas = res.data
    } catch (e) {
      this.error = mensajeDeError(e)
    } finally {
      this.cargando = false
    }
  },
  methods: {
    esActiva,
    formatFecha,
    async cancelar(consulta) {
      if (!confirm(`¿Cancelar la consulta #${consulta.id}?`)) return
      this.procesando = consulta.id
      try {
        const res = await consultasApi.cancelar(consulta.id)
        Object.assign(consulta, res.data)
      } catch (e) {
        alert(mensajeDeError(e))
      } finally {
        this.procesando = null
      }
    }
  }
}
</script>

<style scoped>
.badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}
.badge-pendiente    { background: #fff3cd; color: #856404; }
.badge-confirmado   { background: #d4edda; color: #155724; }
.badge-cancelado    { background: #f8d7da; color: #721c24; }
.badge-atendido     { background: #d1ecf1; color: #0c5460; }
.badge-inasistencia { background: #e2e3e5; color: #383d41; }
.acciones { white-space: nowrap; }
</style>

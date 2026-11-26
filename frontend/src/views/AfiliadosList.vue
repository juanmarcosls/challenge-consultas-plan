<template>
  <div>
    <h2>Afiliados</h2>
    <p v-if="cargando">Cargando...</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <table v-else-if="afiliados.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>Plan</th>
          <th>Nutrición {{ anio }}</th>
          <th>Bloqueado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in afiliados" :key="a.id">
          <td>{{ a.id }}</td>
          <td>{{ a.nombreCompleto }}</td>
          <td>{{ a.dni }}</td>
          <td>{{ a.email }}</td>
          <td>{{ a.telefono }}</td>
          <td>{{ a.plan }}</td>
          <td :title="'Consultas de nutrición presencial que consumen cupo en ' + anio">
            <template v-if="cupos[a.id]">{{ cupos[a.id].usadas }} / {{ cupos[a.id].cupoAnual }}</template>
            <template v-else>—</template>
          </td>
          <td>
            <span v-if="a.bloqueado" style="color: #d32f2f; font-weight: 600">Sí</span>
            <span v-else style="color: #388e3c">No</span>
          </td>
          <td>
            <button class="btn-danger" :disabled="procesando === a.id" @click="eliminar(a)">Dar de baja</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay afiliados registrados.</p>
  </div>
</template>

<script>
import { afiliadosApi, mensajeDeError } from '../services/api'

export default {
  name: 'AfiliadosList',
  data() {
    return {
      afiliados: [],
      cupos: {},
      anio: new Date().getFullYear(),
      cargando: true,
      error: null,
      procesando: null
    }
  },
  async mounted() {
    try {
      const res = await afiliadosApi.getAll()
      this.afiliados = res.data
      this.cargarCupos()
    } catch (e) {
      this.error = mensajeDeError(e)
    } finally {
      this.cargando = false
    }
  },
  methods: {
    async cargarCupos() {
      try {
        const res = await afiliadosApi.getCuposNutricion(this.anio)
        this.cupos = Object.fromEntries(res.data.map(c => [c.afiliadoId, c]))
      } catch {
        // El listado sigue siendo útil sin el cupo; la columna muestra "—".
      }
    },
    async eliminar(afiliado) {
      if (!confirm(`¿Dar de baja a ${afiliado.nombreCompleto}? Su historial de consultas se conserva.`)) return
      this.procesando = afiliado.id
      try {
        await afiliadosApi.delete(afiliado.id)
        this.afiliados = this.afiliados.filter(a => a.id !== afiliado.id)
      } catch (e) {
        alert(mensajeDeError(e))
      } finally {
        this.procesando = null
      }
    }
  }
}
</script>

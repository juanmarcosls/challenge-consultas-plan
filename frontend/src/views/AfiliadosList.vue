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
          <th>Consultas usadas</th>
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
          <td>{{ a.consultasUsadas }}</td>
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
      cargando: true,
      error: null,
      procesando: null
    }
  },
  async mounted() {
    try {
      const res = await afiliadosApi.getAll()
      this.afiliados = res.data
    } catch (e) {
      this.error = mensajeDeError(e)
    } finally {
      this.cargando = false
    }
  },
  methods: {
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

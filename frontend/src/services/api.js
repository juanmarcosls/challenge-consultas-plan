import axios from 'axios'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000'
})

/**
 * Devuelve un mensaje legible a partir de un error de la API.
 * Soporta { mensaje } (errores de negocio) y ProblemDetails de validación ({ errors }).
 */
export function mensajeDeError(error) {
  const data = error?.response?.data
  if (data?.mensaje) return data.mensaje
  if (data?.errors) {
    const primero = Object.values(data.errors).flat()[0]
    if (primero) return primero
  }
  if (error?.response?.status === 404) return 'El recurso solicitado no existe.'
  if (!error?.response) return 'No se pudo conectar con el servidor.'
  return 'Error al procesar la solicitud.'
}

export const consultasApi = {
  getAll:             ()   => http.get('/consultas'),
  getById:            (id) => http.get(`/consultas/${id}`),
  create:             (data) => http.post('/consultas', data),
  confirmar:          (id) => http.post(`/consultas/${id}/confirmar`),
  cancelar:           (id) => http.post(`/consultas/${id}/cancelar`),
  marcarInasistencia: (id) => http.post(`/consultas/${id}/inasistencia`),
  registrarAtencion:  (id) => http.post(`/consultas/${id}/atencion`)
}

export const afiliadosApi = {
  getAll:  ()         => http.get('/afiliados'),
  getById: (id)       => http.get(`/afiliados/${id}`),
  create:  (data)     => http.post('/afiliados', data),
  update:  (id, data) => http.put(`/afiliados/${id}`, data),
  delete:  (id)       => http.delete(`/afiliados/${id}`),
  getCupoNutricion:   (id, anio) => http.get(`/afiliados/${id}/cupo-nutricion`, { params: { anio } }),
  getCuposNutricion:  (anio)     => http.get('/afiliados/cupo-nutricion', { params: { anio } })
}

export const profesionalesApi = {
  getAll: () => http.get('/profesionales')
}

export const sedesApi = {
  getAll: () => http.get('/sedes')
}

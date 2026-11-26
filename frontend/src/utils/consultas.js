// Estados desde los que una consulta todavía admite acciones (los demás son finales).
const ESTADOS_ACTIVOS = ['Pendiente', 'Confirmado']

export const esActiva = (consulta) => ESTADOS_ACTIVOS.includes(consulta?.estado)

export const puedeConfirmar = (consulta) => consulta?.estado === 'Pendiente'

export function formatFecha(fecha) {
  return new Date(fecha).toLocaleString('es-AR')
}

/** Fecha/hora local actual en el formato que espera un input datetime-local (yyyy-MM-ddTHH:mm). */
export function ahoraParaInput() {
  const d = new Date()
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset())
  return d.toISOString().slice(0, 16)
}

const normalizar = (texto) =>
  (texto ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toUpperCase()

/** Misma regla que el backend: compara sin mayúsculas ni tildes. */
export const esNutricion = (especialidad) => normalizar(especialidad) === 'NUTRICION'

export const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio',
  'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre']

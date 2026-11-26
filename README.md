# Consultas de Afiliados

Sistema de gestión de consultas médicas para los afiliados de un plan de salud. Permite registrar afiliados, asignar consultas con profesionales de distintas especialidades y hacer seguimiento del estado de cada consulta.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

---

## Backend

```bash
cd backend
dotnet run
```

El servidor levanta en **http://localhost:5000**.

La base de datos SQLite (`consultas.db`) se crea automáticamente en la primera ejecución junto con los datos iniciales.

**Swagger:** http://localhost:5000/swagger

> Si ya tenías un `consultas.db` de una versión anterior, borralo antes de correr: la base se crea con `EnsureCreated`, que no actualiza el esquema de una base existente.

### Política de cupo de nutrición

El plan incluye 6 consultas de nutrición presencial por año calendario, con un máximo de 1 por mes. Se valida al agendar (`POST /consultas`). Para consultar el cupo:

- `GET /afiliados/{id}/cupo-nutricion?anio=2026`: cupo de un afiliado.
- `GET /afiliados/cupo-nutricion?anio=2026`: cupo de todos los afiliados activos.

### Tests

```bash
dotnet test backend/ConsultasPlan.sln
```

---

## Frontend

```bash
cd frontend
npm install
npm run dev
```

El cliente levanta en **http://localhost:5173**.

La URL de la API se puede configurar con la variable `VITE_API_URL` (ver `frontend/.env.example`). Por defecto usa `http://localhost:5000`.

---

## Estructura del proyecto

```
/
├── backend/     # API REST (.NET 8 · EF Core · SQLite)
├── frontend/    # SPA (Vue 3 · Vite)
└── tests/       # Tests unitarios del backend (xUnit)
```

# Project Pokémon

## Descripción

**Project Pokémon** es una aplicación web multijugador de simulación de combates Pokémon desarrollada como proyecto final del ciclo formativo de Administración de Sistemas Informáticos en Red (ASIR).

La plataforma permite a los usuarios registrarse, crear y gestionar equipos Pokémon, interactuar con otros jugadores y participar en combates en tiempo real. El sistema reproduce las mecánicas de combate de los videojuegos **Pokémon FireRed y Pokémon LeafGreen**, incorporando funcionalidades modernas propias de las aplicaciones web actuales.

---

## Características principales

* Registro e inicio de sesión de usuarios.
* Autenticación mediante JWT y OAuth 2.0.
* Creación, edición y gestión de equipos Pokémon.
* Sistema de amigos y desafíos directos.
* Matchmaking automático entre jugadores.
* Combates multijugador en tiempo real.
* Chat integrado durante los combates.
* Persistencia de datos mediante base de datos relacional.
* Interfaz web responsive adaptada a distintos dispositivos.
* Simulación basada exclusivamente en Pokémon de tercera generación.

---

## Autores
- [Jorge González Jiménez](https://github.com/jor2511)
- [Cristina Cabello Rubio](https://github.com/cristycr)
- [Jesús Felipe Fuentes Trigueros](https://github.com/jffuentes-15)

**Ciclo Formativo:** Administración de Sistemas Informáticos en Red (ASIR)

**Centro Educativo:** IES Miguel Romero Esteo

**Curso Académico:** 2024 - 2026

---

## Tecnologías utilizadas

### Backend

* C#
* ASP.NET Core
* Entity Framework Core
* WebSockets
* JSON Web Tokens (JWT)
* OAuth 2.0

### Frontend

* Angular
* TypeScript
* HTML5
* CSS3

### Base de datos

* SQLite (entorno de desarrollo)
* MariaDB (entorno de producción)

### Infraestructura

* Docker
* Kubernetes
* Proxmox VE
* Debian GNU/Linux
* Nginx
* ASP Monster

### DevOps y Gestión

* Git
* GitHub
* GitHub Projects
* GitHub Actions
* CI/CD
* Web Deploy

---

## Objetivos del proyecto

El objetivo principal de Project Pokémon es desarrollar una plataforma web moderna que permita realizar combates Pokémon multijugador en tiempo real mediante una arquitectura escalable, segura y mantenible.

Además de ofrecer una experiencia de usuario completa, el proyecto sirve como demostración práctica de los conocimientos adquiridos durante el ciclo formativo en áreas como:

* Desarrollo de aplicaciones web.
* Administración de sistemas.
* Bases de datos relacionales.
* Seguridad informática.
* Redes y comunicaciones.
* Virtualización y contenedores.
* Automatización y despliegue continuo.

---

# Guía de ejecución en entorno de desarrollo

Esta sección describe el procedimiento necesario para descargar, configurar y ejecutar el proyecto en un entorno local.

## Requisitos previos

### Software necesario

| Herramienta                   | Requisito                          |
| ----------------------------- | ---------------------------------- |
| .NET SDK                      | Versión utilizada por el proyecto  |
| Node.js                       | Versión LTS compatible             |
| pnpm                          | Última versión estable             |
| Angular CLI                   | Versión compatible con el proyecto |
| Git                           | Última versión estable             |
| Visual Studio 2022 o superior | Recomendado para el backend        |

### Comprobación de versiones

```bash
dotnet --version
node --version
pnpm --version
ng version
git --version
```

---

## Obtención del código fuente

Clonar el repositorio oficial:

```bash
git clone https://github.com/Project-Pokemon/Project-Pokemon.git
cd Project-Pokemon
```

---

## Configuración del backend

### Abrir la solución

Abrir la solución del proyecto:

```text
backend/ProjectPokemon.slnx
```

### Restaurar dependencias

```bash
dotnet restore
```

### Ejecutar la aplicación

```bash
dotnet run
```
---

## Configuración del frontend

Acceder al directorio del frontend:

```bash
cd frontend
```

Instalar dependencias:

```bash
pnpm install
```

Iniciar el servidor de desarrollo:

```bash
pnpm start
```

o

```bash
ng serve
```
---

## Despliegue

La versión pública de la aplicación se encuentra disponible en:

```text
https://projectpokemon.runasp.net
```

---

## Licencia

Proyecto desarrollado con fines educativos como Trabajo Final del Ciclo Formativo de Grado Superior de Administración de Sistemas Informáticos en Red (ASIR).

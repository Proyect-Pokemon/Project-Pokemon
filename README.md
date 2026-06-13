![Project Pokémon](https://raw.githubusercontent.com/Proyect-Pokemon/.github/main/HeaderProjectPokemon.png)
# Project Pokémon
**Project Pokémon** es una aplicación web multijugador de simulación de combates Pokémon desarrollada como proyecto final del ciclo formativo de Administración de Sistemas Informáticos en Red (ASIR).

La plataforma permite a los usuarios registrarse, crear y gestionar equipos Pokémon, interactuar con otros jugadores y participar en combates en tiempo real. El sistema reproduce las mecánicas de combate de los videojuegos **Pokémon FireRed y Pokémon LeafGreen**, incorporando funcionalidades modernas propias de las aplicaciones web actuales.

## 📚 Índice
- [⭐ Project Pokémon](#project-pokémon)
- [👨‍💻 Autores del proyecto](#-autores-del-proyecto)
- [🌐 Enlaces](#-enlaces)
- [🛠️ Tecnologías utilizadas](#-tecnologías-utilizadas)
  - [Backend](#backend)
  - [Frontend](#frontend)
  - [Base de datos](#base-de-datos)
  - [Infraestructura](#infraestructura)
  - [DevOps y Gestión](devops-y-gestión)
- [📗 Memoria](/docs/MemoriaProjectPokemon.pdf)
- [✨ Vista previa](#-vista-previa)
- [📁 Arquitectura de sistemas](#-arquitectura-de-sistemas)
- [📄 Readme Front]()
- [📄 Readme Back]()
- [📄 Readme Base de Datos]()
- [🌐 Bibliografía](#-bibliografía)
- [⚖️ Licencia](#-licencia)

## 👨‍💻 Autores del proyecto
- [Jorge González Jiménez](https://github.com/jor2511)
- [Cristina Cabello Rubio](https://github.com/cristycr)
- [Jesús Felipe Fuentes Trigueros](https://github.com/jffuentes-15)

**Ciclo Formativo:** Administración de Sistemas Informáticos en Red (ASIR)\
**Centro Educativo:** IES Miguel Romero Esteo\
**Curso Académico:** 2024 - 2026

## 🌐 Enlaces
- Versión desplegada: https://projectpokemon.runasp.net
- Vídeo demostrativo: https://youtu.be/cg7Q7EiPu7w

## 🛠️ Tecnologías utilizadas

### Backend
- C#
- ASP.NET Core
- Entity Framework Core
- WebSockets
- JSON Web Tokens (JWT)
- OAuth 2.0

### Frontend
- Angular
- TypeScript
- HTML5
- CSS3

### Base de datos
- SQLite (entorno de desarrollo)
- MariaDB (entorno de producción)

### Infraestructura
- Docker
- Kubernetes
- Proxmox VE
- Debian GNU/Linux
- Nginx
- MonsterASP

### DevOps y Gestión
- Git
- GitHub
- GitHub Projects
- GitHub Actions
- CI/CD
- Web Deploy

## 🎨 Vista previa
<img width="1901" height="911" alt="image" src="https://github.com/user-attachments/assets/2f4ee11f-2ab0-4d4e-9ef4-37132746078a" />
<img width="1901" height="882" alt="image" src="https://github.com/user-attachments/assets/2e2b663e-5b48-45f1-9f24-7a752f59667f" />
<img width="1897" height="916" alt="image" src="https://github.com/user-attachments/assets/836abbdc-ae35-49f2-abf8-e4827adf6341" />
<img width="1901" height="852" alt="image" src="https://github.com/user-attachments/assets/731df3e3-757a-41d4-8da2-2d484ab705f5" />
<img width="1918" height="851" alt="image" src="https://github.com/user-attachments/assets/fceaf5d3-2f55-4841-b972-d100b65795b6" />
<img width="1892" height="917" alt="image" src="https://github.com/user-attachments/assets/fe68add5-01e8-46c9-a441-7688aa396a05" />
<img width="1897" height="912" alt="image" src="https://github.com/user-attachments/assets/61db6db5-5b03-442a-aad0-fddf56409eec" />

## 📁 Arquitectura de sistemas
### Producción (ASP Monster)
```text
┌─────────────────┐
│     Usuario     │
│ Navegador Web   │
└────────┬────────┘
         │ HTTPS
         ▼
┌──────────────────────────┐
│      ASP Monster         │
│                          │
│  Project Pokémon         │
│  ASP.NET + Angular       │
│  WebSockets              │
└────────┬─────────────────┘
         │ Conexión interna
         │
         ▼
┌──────────────────────────┐
│ Servidor de Base de Datos│
│        MariaDB           │
└──────────────────────────┘
```
Los usuarios acceden a la aplicación mediante HTTPS. El servidor web alojado en ASP Monster ejecuta la aplicación Project Pokémon y gestiona tanto las peticiones HTTP como las conexiones WebSocket utilizadas por los combates en tiempo real. La aplicación se comunica internamente con un servidor MariaDB independiente encargado del almacenamiento persistente de los datos.
---

### Arquitectura Virtualizada (Proxmox VE)
```text
┌─────────────────────────────────────────────────────┐
│                Servidor Físico                      │
│                  Proxmox VE                         │
│                                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ VM1K8S                                          │ │
│ │ Kubernetes Control Plane                        │ │
│ │ Ingress Controller                              │ │
│ └───────────────────┬─────────────────────────────┘ │
│                     │                               │
│                     ▼                               │
│ ┌─────────────────────────────────────────────────┐ │
│ │ VM2DWN                                          │ │
│ │ Kubernetes Worker Node                          │ │
│ │ Docker Engine                                   │ │
│ │ Pods Project Pokémon                            │ │
│ │ Frontend Angular                                │ │
│ │ Backend ASP.NET                                 │ │
│ └───────────────────┬─────────────────────────────┘ │
│                     │                               │
│                     ▼                               │
│ ┌─────────────────────────────────────────────────┐ │
│ │ VM3DB                                           │ │
│ │ MariaDB                                         │ │
│ └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
                      ▲
                      │ HTTPS
                      │
                ┌───────────┐
                │ Usuarios  │
                └───────────┘
```
La infraestructura se encuentra desplegada sobre un único servidor físico gestionado mediante Proxmox VE. Dentro del entorno virtualizado se ejecutan tres máquinas virtuales independientes: una máquina destinada al plano de control de Kubernetes e Ingress, una segunda máquina que actúa como nodo trabajador ejecutando los contenedores Docker de la aplicación y una tercera máquina dedicada exclusivamente al servidor MariaDB. Esta separación permite simular una arquitectura distribuida similar a un entorno productivo real.

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

## 🌐 Bibliografía
Documentación sobre Pokémon
Wikidex, la Enciclopedia Pokémon. Cálculo de características.
https://www.wikidex.net/wiki/Caracter%C3%ADsticas#C%C3%A1lculo_de_caracter%C3%ADsticas

Wikidex, la Enciclopedia Pokémon. Cambios en características.
https://www.wikidex.net/wiki/Caracter%C3%ADsticas#Cambios_en_caracter%C3%ADsticas

Wikidex, la Enciclopedia Pokémon. Tiempo atmosférico.
https://www.wikidex.net/wiki/Tiempo_atmosf%C3%A9rico

Wikidex, la Enciclopedia Pokémon. Lista de Pokémon de la primera generación.
https://www.wikidex.net/wiki/Lista_de_Pok%C3%A9mon_de_la_primera_generaci%C3%B3n

Wikidex, la Enciclopedia Pokémon. Lista de movimientos.
https://www.wikidex.net/wiki/Lista_de_movimientos#Lista_de_movimientos 

Wikidex, la Enciclopedia Pokémon. Lista de movimientos por alteración de estado.
https://www.wikidex.net/wiki/Lista_de_movimientos_por_alteraci%C3%B3n_de_estado 

Wikidex, la Enciclopedia Pokémon. Estados.
https://www.wikidex.net/wiki/Estado 

Wikidex, la Enciclopedia Pokémon. Índice de golpe crítico.
https://www.wikidex.net/wiki/Golpe_cr%C3%ADtico#%C3%8Dndice_de_golpe_cr%C3%ADtico 

Wikidex, la Enciclopedia Pokémon. Cálculo del poder oculto.
https://www.wikidex.net/wiki/C%C3%A1lculo_del_poder_oculto 

Wikidex, la Enciclopedia Pokémon. Características de los movimientos.
https://www.wikidex.net/wiki/Movimiento#Caracter%C3%ADsticas 

Wikidex, la Enciclopedia Pokémon. Lista de objetos de tercera generación.
https://www.wikidex.net/wiki/Lista_de_objetos_por_n%C3%BAmero_de_%C3%ADndice_de_la_tercera_generaci%C3%B3n

Bulbapedia, la Enciclopedia Pokémon Movida por la Comunidad. Lista de Pokémon por estadísticas base en generaciones II-V.
https://bulbapedia.bulbagarden.net/wiki/List_of_Pok%C3%A9mon_by_base_stats_in_Generations_II-V 

PokéAPI, la API de Pokémon RESTful. Documentación.
https://pokeapi.co/docs/v2 

Pokémon Showdown, Simulador de Combates Pokémon. Repositorio de GitHub de Pokémon Showdown.
https://github.com/smogon/Pokémon-showdown/tree/master 

Otra Documentación

W3schools, la Página de Aprendizaje de Tecnologías Web Más Completa. Documentación sobre HTML5.
https://www.w3schools.com/html/default.asp

W3schools, la Página de Aprendizaje de Tecnologías Web Más 
Completa. Documentación sobre CSS3.
https://www.w3schools.com/css/default.asp

W3schools, la Página de Aprendizaje de Tecnologías Web Más Completa. Documentación sobre JS.
https://www.w3schools.com/js/default.asp

W3schools, la Página de Aprendizaje de Tecnologías Web Más Completa. Documentación sobre C#.
https://www.w3schools.com/cs/index.php

W3schools, la Página de Aprendizaje de Tecnologías Web Más Completa. Documentación sobre ASP.
https://www.w3schools.com/asp/default.asp 

W3schools, la Página de Aprendizaje de Tecnologías Web Más Completa. Ciberseguridad.
https://www.w3schools.com/cybersecurity/index.php 

Microsoft, Empresa Tecnológica Multinacional. Documentación sobre ASP oficial.
https://dotnet.microsoft.com/es-es/ 

Microsoft, Empresa Tecnológica Multinacional. Documentación sobre C# oficial.
https://learn.microsoft.com/es-es/dotnet/csharp/ 

WebSocket, Comunicación Bidireccional y Full-Duplex. Documentación oficial sobre WebSockets.
https://websockets.spec.whatwg.org/ 

Wikipedia, la Enciclopedia Libre. WebSocket.
https://en.wikipedia.org/wiki/WebSocket 

SockJS, Librería de JavaScript. Perfil oficial de GitHub de SockJS.
https://github.com/sockjs 

GitHub, la Plataforma de Proyectos con Git. Sobre los repositorios.
https://docs.github.com/en/repositories/creating-and-managing-repositories/about-repositories 

GitHub, la Plataforma de Proyectos con Git. Sobre Projects.
https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/about-projects

GitHub, la Plataforma de Proyectos con Git. Empezando con Projects.
https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/quickstart-for-projects 

GitHub,  la Plataforma de Proyectos con Git. Buenas prácticas con Projects.
https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/best-practices-for-projects 

ISO/IEC 27001:2022, Information security, cybersecurity and privacy protection.
Ley Orgánica 3/2018, de 5 de diciembre, de Protección de Datos Personales y garantía de los derechos digitales.

## ⚖️ Licencia
Proyecto desarrollado con fines educativos como Trabajo Final del Ciclo Formativo de Grado Superior de Administración de Sistemas Informáticos en Red (ASIR).

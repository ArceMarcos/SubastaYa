
Plataforma de subastas en tiempo real con sistema de billetera virtual, desarrollada como Trabajo Práctico de Proyecto de Software.
Tecnologias utilizadas:
* Backend: .NET 9 (C#), ASP.NET Core Web API.
* Base de Datos: PostgreSQL con Entity Framework Core.
* Frontend: HTML5, CSS3 y Vanilla JavaScript.
* Infraestructura: Docker & Docker Compose.

* Autenticación Segura: Sistema de Login y Registro protegido mediante tokens JWT (arquitectura Stateless).
* Concurrencia Optimista: Manejo de colisiones en la base de datos mediante la columna `xmin` de PostgreSQL, garantizando la integridad de los datos si múltiples usuarios pujan en el mismo milisegundo.
* Procesos en Segundo Plano: Servicio asíncrono (`BackgroundService`) que monitorea y cierra automáticamente las subastas expiradas.
* Billetera Virtual: Gestión de saldos y depósitos integrados a la lógica de pujas.

Requisitos previos: Tener instalado [Docker Desktop](https://www.docker.com/products/docker-desktop/) y Git.

   ```bash
   git clone [https://github.com/ArceMarcos/SubastaYa.git](https://github.com/ArceMarcos/SubastaYa.git)
   cd subasta
   docker-compose up --build -d

Para realizar una prueba de concurrencia se utilizo el siguiente script:


const tokenUsuario1 = "Token";
const tokenUsuario2 = "Token";

const idSubasta = 1; // El ID de la subasta activa
const montoPuja = 0000; // Ambos van a ofertar lo mismo

// peticiones
const peticion1 = fetch(`http://localhost:5000/api/subastas/${idSubasta}/pujar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${tokenUsuario1}` },
    body: JSON.stringify({ monto: montoPuja })
});

const peticion2 = fetch(`http://localhost:5000/api/subastas/${idSubasta}/pujar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${tokenUsuario2}` },
    body: JSON.stringify({ monto: montoPuja })
});

console.log("¡Disparando peticiones simultáneas!");

// Disparamos AMBAS peticiones exactamente al mismo tiempo
Promise.all([peticion1, peticion2])
    .then(async (respuestas) => {
        const texto1 = await respuestas[0].text();
        const texto2 = await respuestas[1].text();
        
        console.log("=== RESULTADOS ===");
        console.log(`Usuario 1 (Código ${respuestas[0].status}):`, texto1);
        console.log(`Usuario 2 (Código ${respuestas[1].status}):`, texto2);
    });

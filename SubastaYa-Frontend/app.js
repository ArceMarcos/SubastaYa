document.getElementById('loginForm').addEventListener('submit', async function(evento) {
    // 1. Evitamos que la página se recargue al enviar el formulario
    evento.preventDefault(); 

    // 2. Capturamos los datos que escribió el usuario
    const correo = document.getElementById('correo').value;
    const contrasena = document.getElementById('contrasena').value;
    const mensajeError = document.getElementById('mensajeError');

    try {
        // 3. Enviamos la petición POST a nuestra API en .NET
        const respuesta = await fetch('http://localhost:5000/api/usuarios/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                correoElectronico: correo, 
                contrasena: contrasena 
            })
        });

        if (respuesta.ok) {
            // 4. Si el login es correcto, extraemos el Token JWT
            const datos = await respuesta.json();
            
            // Guardamos el token en la memoria del navegador (LocalStorage)
            localStorage.setItem('tokenSubasta', datos.token);
            
            alert("¡Login exitoso! Token guardado de forma segura.");
            window.location.href = 'subastas.html';
        } else {
            // 5. Si rebota (Error 401), mostramos el mensaje rojo
            mensajeError.style.display = 'block';
        }
    } catch (error) {
        console.error("Error al conectar con la API:", error);
        alert("No se pudo conectar con el servidor.");
    }
});
document.getElementById('formRegistro').addEventListener('submit', async function(evento) {
    evento.preventDefault(); 

    const nombre = document.getElementById('nombre').value;
    const correo = document.getElementById('correo').value;
    const contrasena = document.getElementById('contrasena').value;
    const mensajeError = document.getElementById('mensajeError');

    try {
        const respuesta = await fetch('http://localhost:5000/api/usuarios/registro', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ 
                nombre: nombre,
                correoElectronico: correo, 
                contrasenaHash: contrasena // Tu backend lo encriptará
            })
        });

        if (respuesta.ok) {
            alert("¡Cuenta creada con éxito! Ahora puedes iniciar sesión.");
            window.location.href = 'index.html'; // Lo mandamos al Login
        } else {
            const errorTexto = await respuesta.text();
            mensajeError.innerText = errorTexto || "Error al registrar el usuario.";
            mensajeError.style.display = 'block';
        }
    } catch (error) {
        console.error("Error de conexión:", error);
        mensajeError.innerText = "No se pudo conectar con el servidor.";
        mensajeError.style.display = 'block';
    }
});
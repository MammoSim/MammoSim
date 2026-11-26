import random
from locust import HttpUser, task, between
IDS = None
class Estudiante(HttpUser):
    wait_time = between(0.5, 1.5)
    def on_start(self):
        global IDS
        if IDS is None:
            IDS = [c["id_caso"] for c in self.client.get("/casos").json()]
    @task(1)
    def salud(self): self.client.get("/salud", name="GET /salud")
    @task(4)
    def catalogo(self): self.client.get("/casos", name="GET /casos")
    @task(4)
    def caso(self): self.client.get(f"/casos/{random.choice(IDS)}", name="GET /casos/{id}")
    @task(2)
    def perfil(self): self.client.get("/usuarios/me", name="GET /usuarios/me")
    @task(2)
    def mias(self): self.client.get("/sesiones/mias", name="GET /sesiones/mias")

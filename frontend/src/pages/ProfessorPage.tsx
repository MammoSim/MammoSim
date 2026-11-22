import { useEffect, useState, type FormEvent } from "react";
import {
  createCourse,
  getMyCourse,
  type Course,
} from "../services/coursesServices";
import "../styles/ProfessorPage.css";
import SidebarProfessor from "../components/layout/SidebarProfessor";
import { useProfile } from "../hooks/useProfile";
import PageHeader from "../components/layout/PageHeader";

export default function ProfessorPage() {
  const [course, setCourse] = useState<Course | null>(null);
  const [name, setName] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadFailed, setLoadFailed] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const {
    usuario,
    loading: profileLoading,
    error: profileError,
  } = useProfile();

  useEffect(() => {
    const controller = new AbortController();

    async function loadCourse() {
      try {
        const data = await getMyCourse(controller.signal);

        if (!controller.signal.aborted) {
          setCourse(data);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setLoadFailed(true);
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible consultar tu clase",
          );
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }
    void loadCourse();

    return () => controller.abort();
  }, []);

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (submitting) return;

    const nombre = name.trim();

    if (nombre.length < 5) {
      setError("Escribe un nombre de al menos 5 caracteres");
      return;
    }

    setSubmitting(true);
    setError("");

    try {
      const created = await createCourse(nombre);
      setCourse(created);
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : "No fue posible crear la clase",
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="sidebar_container">
      <SidebarProfessor />

      <main className="professor_page">
        {/* Cargar la información del perfil */}
        {profileLoading ? (
          <p role="status">Cargando perfil...</p>
        ) : profileError ? (
          <p role="alert">{profileError}</p>
        ) : usuario ? (
          <PageHeader
            title="Aquí verás tu clase"
            description="El código permite que tu estudiantes ingresen a tu clase para que puedas evaluar su interacción en los casos presentados"
            name={usuario.nombre_completo}
            carrera={usuario.carrera}
          />
        ) : null}

        {loading ? (
          <p role="status">Consultando clase...</p>
        ) : course ? (
          <section className="professor_card">
            <h2>{course.nombre}</h2>
            <p>
              Comparte este código con tus estudiantes, para visualizar sus
              interacciones
            </p>

            <p className="course_code">{course.codigo}</p>
          </section>
        ) : !loadFailed ? (
          <section>
            <h2>Crea tu clase</h2>
            <p>Para tener una clase asociada a tu cuenta</p>
            <form className="professor_form" onSubmit={handleCreate}>
              <label htmlFor="course-name">Nombre de la clase</label>
              <input
                id="course-name"
                name="nombre"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Ejemplo: Radiología - RA001"
                minLength={5}
                required
              />

              <button type="submit" disabled={submitting}>
                {submitting ? "Creando clase..." : "Crear clase"}
              </button>
            </form>
          </section>
        ) : null}

        {error && <p role="alert">{error}</p>}
      </main>
    </div>
  );
}

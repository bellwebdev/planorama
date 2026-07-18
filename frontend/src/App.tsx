import styles from './App.module.css'

function App() {
  return (
    <main className={styles.shell}>
      <header className={styles.header}>
        <h1>Planorama</h1>
        <p className={styles.tagline}>Plan trips together, vote on what matters.</p>
      </header>
      <section className={styles.placeholder}>
        <p>Phase 0 scaffold — trips, suggestions, and voting arrive in later phases.</p>
      </section>
    </main>
  )
}

export default App

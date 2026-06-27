export default function Header() {
  return (
    <header className="shrink-0 h-28 bg-gray-900 border-b border-gray-700 flex items-center px-4 z-50">
      <div className="flex items-center gap-3">
        <img src="/cg-logo.png" alt="CrashGuard.io" className="w-16 h-16" />
        <span className="text-white font-semibold text-lg tracking-tight">CrashGuard.io</span>
      </div>
    </header>
  )
}

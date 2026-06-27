import { type ReactNode } from 'react'
import Header from './Header'
import Sidebar from './Sidebar'

export default function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="h-screen w-screen overflow-hidden bg-gray-950 text-gray-100 flex flex-col">
      <Header />
      <div className="flex-1 flex min-h-0">
        <Sidebar />
        <main className="flex-1 min-h-0 p-6 flex flex-col overflow-y-auto">
          {children}
        </main>
      </div>
    </div>
  )
}

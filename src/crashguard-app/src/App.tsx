import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import CanaryDetailPage from './pages/CanaryDetailPage'
import CanaryTypeTriggersPage from './pages/CanaryTypeTriggersPage'
import CanariesPage from './pages/CanariesPage'
import CanaryTypesPage from './pages/CanaryTypesPage'
import CanaryEditorPage from './pages/CanaryEditorPage'
import ChannelsPage from './pages/ChannelsPage'
import ChannelEditorPage from './pages/ChannelEditorPage'
import SettingsPage from './pages/SettingsPage'

const queryClient = new QueryClient()

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/canaries" element={<CanariesPage />} />
            <Route path="/canaries/:canaryType/:referenceId" element={<CanaryDetailPage />} />
            <Route path="/canary-types" element={<CanaryTypesPage />} />
            <Route path="/canary-types/:canaryType/triggers" element={<CanaryTypeTriggersPage />} />
            <Route path="/canary-types/new" element={<CanaryEditorPage />} />
            <Route path="/canary-types/:id/edit" element={<CanaryEditorPage />} />
            <Route path="/channels" element={<ChannelsPage />} />
            <Route path="/channels/new" element={<ChannelEditorPage />} />
            <Route path="/channels/:id/edit" element={<ChannelEditorPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

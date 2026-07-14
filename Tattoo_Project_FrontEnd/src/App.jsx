import { Navigate, Route, Routes } from "react-router-dom";
import Navbar from "./components/Navbar";
import ProtectedRoute from "./components/ProtectedRoute";
import HomePage from "./pages/HomePage";
import RegisterPage from "./pages/RegisterPage";
import LoginPage from "./pages/LoginPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage";
import ChooseProfileTypePage from "./pages/ChooseProfileTypePage";
import CreateClientProfilePage from "./pages/CreateClientProfilePage";
import CreateArtistProfilePage from "./pages/CreateArtistProfilePage";
import ArtistsPage from "./pages/ArtistsPage";
import CreateTattooRequestPage from "./pages/CreateTattooRequestPage";
import MyTattooRequestsPage from "./pages/MyTattooRequestsPage";
import CreateConsultationPage from "./pages/CreateConsultationPage";
import BookTattooSessionPage from "./pages/BookTattooSessionPage";
import ArtistWorkspacePage from "./pages/ArtistWorkspacePage";
import CreateArtistResponsePage from "./pages/CreateArtistResponsePage";
import CompleteConsultationPage from "./pages/CompleteConsultationPage";
import AddMoreSessionsPage from "./pages/AddMoreSessionsPage";
import CompleteTattooPage from "./pages/CompleteTattooPage";
import FavoriteArtistsPage from "./pages/FavoriteArtistsPage";
import CreateArtistReviewPage from "./pages/CreateArtistReviewPage";
import ArtistRequestsPage from "./pages/ArtistRequestsPage";
import ArtistSchedulePage from "./pages/ArtistSchedulePage";
import ProfileSectionPage from "./pages/ProfileSectionPage";
import ArtistPortfolioPage from "./pages/ArtistPortfolioPage";
import AiStudioPage from "./pages/AiStudioPage";
import CreateAiTattooPage from "./pages/CreateAiTattooPage";
import AiTattooProjectPage from "./pages/AiTattooProjectPage";

function App() {
  return (
    <>
      <Navbar />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/choose-profile" element={<ProtectedRoute><ChooseProfileTypePage /></ProtectedRoute>} />
        <Route path="/create-client-profile" element={<ProtectedRoute><CreateClientProfilePage /></ProtectedRoute>} />
        <Route path="/create-artist-profile" element={<ProtectedRoute><CreateArtistProfilePage /></ProtectedRoute>} />
        <Route path="/profile/:section" element={<ProtectedRoute><ProfileSectionPage /></ProtectedRoute>} />
        <Route path="/profile" element={<Navigate to="/profile/user" replace />} />

        <Route path="/explore" element={<ArtistsPage />} />
        <Route path="/ai-studio" element={<ProtectedRoute roles={["Client", "TattooArtist"]}><AiStudioPage /></ProtectedRoute>} />
        <Route path="/ai-studio/new" element={<ProtectedRoute roles={["Client", "TattooArtist"]}><CreateAiTattooPage /></ProtectedRoute>} />
        <Route path="/ai-studio/:projectId" element={<ProtectedRoute roles={["Client", "TattooArtist"]}><AiTattooProjectPage /></ProtectedRoute>} />
        <Route path="/artists" element={<Navigate to="/explore" replace />} />
        <Route path="/artists/:artistId/portfolio" element={<ArtistPortfolioPage />} />

        <Route path="/create-tattoo-request/:artistId?" element={<ProtectedRoute roles={["Client"]}><CreateTattooRequestPage /></ProtectedRoute>} />
        <Route path="/bookings" element={<ProtectedRoute roles={["Client"]}><MyTattooRequestsPage /></ProtectedRoute>} />
        <Route path="/my-requests" element={<Navigate to="/bookings" replace />} />
        <Route path="/favorites" element={<ProtectedRoute roles={["Client"]}><FavoriteArtistsPage /></ProtectedRoute>} />
        <Route path="/review/:tattooRequestId" element={<ProtectedRoute roles={["Client"]}><CreateArtistReviewPage /></ProtectedRoute>} />
        <Route path="/book-consultation/:tattooRequestId?" element={<ProtectedRoute roles={["Client"]}><CreateConsultationPage /></ProtectedRoute>} />
        <Route path="/book-session/:tattooRequestId?" element={<ProtectedRoute roles={["Client"]}><BookTattooSessionPage /></ProtectedRoute>} />

        <Route path="/my-studio" element={<ProtectedRoute roles={["TattooArtist"]}><ArtistWorkspacePage /></ProtectedRoute>} />
        <Route path="/artist-workspace" element={<Navigate to="/my-studio" replace />} />
        <Route path="/my-studio/requests" element={<ProtectedRoute roles={["TattooArtist"]}><ArtistRequestsPage /></ProtectedRoute>} />
        <Route path="/artist-requests" element={<Navigate to="/my-studio/requests" replace />} />
        <Route path="/my-studio/calendar" element={<ProtectedRoute roles={["TattooArtist"]}><ArtistSchedulePage /></ProtectedRoute>} />
        <Route path="/artist-schedule" element={<Navigate to="/my-studio/calendar" replace />} />

        <Route path="/artist-response/:tattooRequestId?" element={<ProtectedRoute roles={["TattooArtist"]}><CreateArtistResponsePage /></ProtectedRoute>} />
        <Route path="/complete-consultation/:tattooRequestId?" element={<ProtectedRoute roles={["TattooArtist"]}><CompleteConsultationPage /></ProtectedRoute>} />
        <Route path="/add-more-sessions/:tattooRequestId?" element={<ProtectedRoute roles={["TattooArtist"]}><AddMoreSessionsPage /></ProtectedRoute>} />
        <Route path="/complete-tattoo/:tattooRequestId?" element={<ProtectedRoute roles={["TattooArtist"]}><CompleteTattooPage /></ProtectedRoute>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  );
}

export default App;

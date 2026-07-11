import ClientImage from './assets/clientImage.jpg'

function CardClients() {
    return (
        <div>
            <img src={ClientImage}></img>
            <h3>Martin Daskalov</h3>
            <p>Im a client</p>
        </div>
    )
}

export default CardClients